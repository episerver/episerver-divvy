using Episerver.Labs.Divvy.Config;
using Episerver.Labs.Divvy.Models;
using Episerver.Labs.Divvy.Webhooks;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Editor;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI.WebControls;

namespace Episerver.Labs.Divvy
{
    public static class DivvyManager
    {
        public static class Settings
        {
            // These can be changed/configured on start-up
            public static string DivvyNotificationEndpoint = "https://app.divvyhq.com/api/2.0/contentitems/webhook_update_status/";
            public static string PublishedStatusLabel = "published";
            public static string DeletedStatusLabel = "killed";
            public static int WebhookTimerInterval = 10000;
            public static DivvyMode Mode = DivvyMode.Disabled;
            public static Guid AccessToken;
            public static Func<IContent, string> PreviewProvider = GetPreviewHtml;
            public static string DebugRole = null;
            public static bool LogRequestDebugData = false;

            public static bool InitComplete = false;
            private static readonly List<DivvyTypeMapping> typeMappings;
            public static ReadOnlyCollection<DivvyTypeMapping> TypeMappings => new ReadOnlyCollection<DivvyTypeMapping>(typeMappings);

            static Settings()
            {
                typeMappings = new List<DivvyTypeMapping>();
            }

            public static void AddMapping(DivvyTypeMapping mapping)
            {
                typeMappings.Add(mapping);
            }


            public static DivvyTypeMapping GetMapping(string divvyContentType)
            {
                return typeMappings.FirstOrDefault(t => t.DivvyTypeName.ToLower() == divvyContentType.ToLower());
            }
        }

        public static event EventHandler<DivvyEventArgs> OnBeforeParseContentGatewayRequest = delegate { };
        public static event EventHandler<DivvyEventArgs> OnAfterParseContentGatewayRequest = delegate { };
        public static event EventHandler<DivvyEventArgs> OnBeforeContentCreation = delegate { };
        public static event EventHandler<DivvyEventArgs> OnAfterGeneratePreviewHtml = delegate { };

        private static readonly IContentRepository repo;
        private static readonly UrlResolver resolver;
        private static readonly IContentTypeRepository typeRepo;

        static DivvyManager()
        {
            repo = ServiceLocator.Current.GetInstance<IContentRepository>();
            resolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            typeRepo = ServiceLocator.Current.GetInstance<IContentTypeRepository>();
        }

        public static void Init()
        {
            if (Settings.InitComplete)
            {
                return;
            }

            // This reads everything from the web.config
            var configSection = (DivvyConfigSection)ConfigurationManager.GetSection("divvy");

            if (configSection == null)
            {
                // No config means we're disabled
                Settings.Mode = DivvyMode.Disabled;
                DivvyLogManager.Log("No config found in web.config. System disabled.");
                return;
            }

            if (configSection.Enabled)
            {
                Settings.Mode = DivvyMode.Enabled;
            }
            else
            {
                // If we're disabled, nothing below matters
                return;
            }

            Settings.AccessToken = configSection.AuthToken;
            if(Settings.AccessToken == null || Settings.AccessToken == Guid.Empty)
            {
                // If they didn't set the access token, we disable
                Settings.Mode = DivvyMode.Disabled;
                DivvyLogManager.Log("No access token found in config. System disabled.");
                return;
            }

            Settings.DebugRole = configSection.DebugRole;

            foreach (DivvyMappingConfig mapping in configSection.Mappings)
            {
                Settings.AddMapping(new DivvyTypeMapping()
                {
                    DivvyTypeName = mapping.DivvyType,
                    EpiserverPageTypeName = mapping.EpiserverType,
                    ParentNode = mapping.Parent
                }); ;
            }

            Settings.InitComplete = true;
        }

        // This is the main method of the class.
        // This processes the inbound Divvy information.
        public static ContentResponse ProcessDivvyInput(string input)
        {
            // Filter the raw string
            input = FilterInput(input);
            if (input == null)
            {
                // Explicit abandon
                return null;
            }

            // Parse the string into an object
            var contentRequest = ParseContentRequest(input);
            if (contentRequest == null)
            {
                // Explicit abandon
                return null;
            }

            // Attempt to find the content mapping
            var contentMapping = FindContentMapping(contentRequest.Id);

            // Do we have an archived mapping?
            // This ensures the same content isn't created twice
            if (contentMapping != null && contentMapping.Status == DivvyContentMapping.ArchivedStatusLabel)
            {
                DivvyLogManager.LogRequest("Found Archived Content Mapping", new { contentMapping.EpiserverContentId, contentMapping.ArchivedOn });
                return null;
            }

            IContent content;
            if (contentMapping == null)
            {
                // No existing content; attempt to find a mapping
                var typeMapping = FindTypeMapping(contentRequest.ContentType);
                if (typeMapping == null)
                {
                    // No existing content and no mapping for new content
                    // We're done here...
                    return null;
                }

                // We found a mapping, create new content
                content = CreateContent(contentRequest, typeMapping);

                if (content == null)
                {
                    // Content was short-circuited in an event handler
                    return null;
                }
            }
            else
            {
                // We found a mapping. Retrieve the content
                var repo = ServiceLocator.Current.GetInstance<IContentRepository>();
                content = repo.Get<IContent>(new ContentReference(contentMapping.EpiserverContentId));

                if (content == null)
                {
                    // For some reason, this content doesn't exist
                    // Archive the mapping
                    DivvyLogManager.LogRequest("Unable to Find Mapped Content", new { EpiserverID = contentMapping.EpiserverContentId });
                    DivvyContentMapping.Archive(contentMapping);
                    return null;
                }

                DivvyLogManager.LogRequest($"Found Episerver Content", new { Id = content.ContentGuid });
            }

            // If we get to this point, we should have content, one way or the other -- either found or created

            // Get and filter the HTML we're sending back for a preview
            var previewHtml = Settings.PreviewProvider(content);
            previewHtml = FilterPreviewHtml(previewHtml);

            // Get the URL for the content in Edit Mode
            var editUrl = GetEditUrl(content);

            // Get the calculated last modified date
            var lastModified = GetLastModifiedDate(content);

            return new ContentResponse()
            {
                Id = content.ContentGuid,
                PreviewHtml = previewHtml,
                EditUrl = editUrl,
                LastModified = lastModified,
                Media = GetMedia(content).Select(m => new Media()
                {
                    Name = m.Name,
                    Url = resolver.GetUrl(m)
                }).ToList()
            };

        }

        private static ContentRequest ParseContentRequest(string input)
        {
            var contentRequest = ContentRequest.ParseJson(input);

            // Filter parsed value
            var e = new DivvyEventArgs() { ContentRequest = contentRequest };
            OnAfterParseContentGatewayRequest(null, e);

            if (e.CancelAction)
            {
                return null;
            }

            return contentRequest;
        }

        private static string FilterInput(string input)
        {
            // Filter the raw input
            var e = new DivvyEventArgs() { RawInput = input };
            OnBeforeParseContentGatewayRequest(null, e);

            if (e.CancelAction)
            {
                return null;
            }

            return e.RawInput;
        }

        private static IContent CreateContent(ContentRequest contentRequest, DivvyTypeMapping mapping)
        {
            // Run the event. The target node might change in here
            var e = new DivvyEventArgs() {
                ContentRequest = contentRequest,
                IntendedParent = mapping.ParentNode,
                IntendedTypeName = mapping.EpiserverPageTypeName
            };
            OnBeforeContentCreation(null, e);

            if (e.CancelAction)
            {
                return null;
            }

            var parent = repo.Get<PageData>(e.IntendedParent);

            DivvyLogManager.LogRequest($"Creating New Content", new { Type = e.IntendedTypeName, ParentId = parent.ContentGuid, ParentName = parent.Name });

            try
            {
                // Get the type ID. For whatever reason, we can't create content with a type name, we have to have the ID...
                var type = typeRepo.Load(e.IntendedTypeName);

                // Create the content
                var content = repo.GetDefault<IContent>(e.IntendedParent, type.ID);
                content.Name = contentRequest.Title;
                repo.Save(content, AccessLevel.NoAccess);

                // There's an edge case where we have a mappng already because the Episerver content got deleted
                if (!DivvyContentMapping.HasDivvyMapping(content.ContentLink.ID))
                {
                    DivvyContentMapping.Create(content.ContentLink.ID, contentRequest.Id);
                }

                DivvyLogManager.LogRequest("Created New Content", new { Id = content.ContentGuid });

                return content;
            }
            catch (Exception ex)
            {
                DivvyLogManager.LogRequest($"Error Creating New Content", ex);
                return null;
            }
        }

        private static DivvyTypeMapping FindTypeMapping(string divvyContentType)
        {
            var mapping = Settings.GetMapping(divvyContentType);
            if (mapping == null)
            {
                DivvyLogManager.LogRequest("No Type Mapping Found");
                return null;
            }
            else
            {
                DivvyLogManager.LogRequest("Found Type Mapping", new { Type = mapping.EpiserverPageTypeName });
                return mapping;
            }
        }

        private static DivvyContentMapping FindContentMapping(int divvyId)
        {
            var divvyContentMapping = DivvyContentMapping.FindFromDivvyContent(divvyId);
            if (divvyContentMapping == null)
            {
                DivvyLogManager.LogRequest("No Existing Content Found");
                return null;
            }

            return divvyContentMapping;
        }

        private static string GetPreviewHtml(IContent content)
        {
            // Clearly, you're going to want to re-implement this
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Episerver.Labs.Divvy.preview-template.html");
            using (var reader = new StreamReader(stream))
            {
                string template = reader.ReadToEnd();
                var title = content.Name;
                var body = content.Property["MainBody"].ToString();
                return template.Replace("{title}", title).Replace("{body}", body);
            }
        }

        private static string FilterPreviewHtml(string html)
        {
            var e = new DivvyEventArgs() { PreviewHtml = html };
            OnAfterGeneratePreviewHtml(null, e);
            return e.PreviewHtml;
        }

        private static string GetEditUrl(IContent content)
        {
            var relativeUrl = PageEditing.GetEditUrl(content.ContentLink);
            var absoluteUrl = new UrlBuilder(HttpContext.Current.Request.Url)
            {
                Path = relativeUrl,
                Query = null // Had to do this because if the auth token is passed in the querystring, it was getting copied to the edit URL...

            };
            return absoluteUrl.Uri.AbsoluteUri;
        }

        // Not used. Might be used in the future.
        private static string GetPublicUrl(IContent content)
        {
            var relativeUrl = resolver.GetUrl(content);
            var absoluteUrl = new UrlBuilder(HttpContext.Current.Request.Url)
            {
                Path = relativeUrl
            };
            return absoluteUrl.Uri.AbsoluteUri;
        }

        private static List<MediaData> GetMedia(IContent content)
        {
            var contentAssetHelper = ServiceLocator.Current.GetInstance<ContentAssetHelper>();
            var contentAssetFolder = contentAssetHelper.GetOrCreateAssetFolder(content.ContentLink);

            return repo.GetChildren<MediaData>(contentAssetFolder.ContentLink).ToList();
        }

        private static DateTime GetLastModifiedDate(IContent content)
        {
            var lastModifiedProperty = content.Property["PageChanged"].Value ?? content.Property["PageCreated"].Value;
            return (DateTime)lastModifiedProperty;
        }

        public static void HandlePublish(object sender, ContentEventArgs e)
        {
            var contentMapping = DivvyContentMapping.FindFromEpiserverContent(e.Content.ContentLink.ID);

            if (contentMapping != null)
            {
                DivvyLogManager.Log("Published Event Raised", new { contentMapping.EpiserverContentId, contentMapping.DivvyId });
                NotifyDivvy(e.Content, Settings.PublishedStatusLabel);
            }
        }

        public static void HandleDelete(object sender, DeleteContentEventArgs e)
        {
            var beingDeleted = new List<ContentReference>();
            if (e.ContentLink != null)
            {
                beingDeleted.Add(e.ContentLink);
            }
            beingDeleted.AddRange(e.DeletedDescendents);

            foreach (var item in beingDeleted)
            {
                var contentMapping = DivvyContentMapping.FindFromEpiserverContent(item.ID);

                if (contentMapping != null)
                {
                    DivvyContentMapping.Archive(contentMapping);

                    DivvyLogManager.Log("Deleted Event Raised", new { EpiserverId = item.ID, DivvyId = DivvyContentMapping.FindFromEpiserverContent(item.ID).DivvyId });

                    var content = repo.Get<IContent>(new ContentReference(item.ID));
                    NotifyDivvy(content, Settings.DeletedStatusLabel);
                }
            }
        }

        private static void NotifyDivvy(IContent content, string status)
        {
            DivvyWebhookManager.Instance.Add(new DivvyWebhook()
            {
                Uri = new Uri(Settings.DivvyNotificationEndpoint),
                Data = new
                {
                    id = DivvyContentMapping.FindFromEpiserverContent(content.ContentLink.ID).DivvyId,
                    state = status
                }
            });
        }
    }
}
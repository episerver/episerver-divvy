# Episerver Divvy Integration

This add-on allows a Divvy account to interact with an Episerver instance. When editrs Create content in the Divvy account, corresponding content objects will be created in the Episerver instance. When previewing content, Divvy will retrieve preview HTML directly from Episerver, and provide a way to enter Episerver's Edit Mode directly from Divvy.

## Installation

This add-on also requires enablement of your Divvy account.

To enable in Divvy:

1. Access "Integration Admin"
2. Select "Episerver" in the left menu
3. Enter the domain name of your Episerver website
4. Enter the auth token value from your web.config (this should be a 32-digit GUID; anything from http://givemeguid.com/ should be fine)
5. Select the content types which should be synchronized to Episerver content

To enable in Episerver:

1. Compile the source into your project.
2. In `/configuration/configSections` add the configuration element as shown below
3. in `/configuration` add the Divvy element as shown below
2. In the web.config, on the `divvy` element, add the same `authToken` from Step #4 above
2. In the web.config, add a "mapping" element for for Divvy content type that should result in the synchronization of an Episerver content object (see below)
3. For each content type that should be created from Divvy, extend the class from `IDivvyMappedType` and implement the `DivvyId` and `GetDivvyPreview` members

### Config Section Element

```
<section name="divvy" type="Episerver.Divvy.Config.DivvyConfigSection"/>
```

### Divvy Element

```
<divvy enabled="true" debug="false" authToken="[the unique auth token]">
   <mapping divvyType="[name of content type in Divvy]" episerverType="[name of Episerver content type]" parent="[the numeric ID of where the content should be created]"/>
</divvy>
```

## Process

When the Episerver integration is enabled in Divvy, an API request is sent to Episerver _every time_ new content is created in Divvy of one of the content types specified during configuration.

If the Episerver instance is not configured to handle the specific content type created, Episerver returns a `null` value to Divvy, and Divvy functions normally.

If the Episerver instance *is* configured to handle the specific content type created (via a 'mapping' element, as shown above), the Episerver instance will create a draft object of the specifed type, store the Divvy ID with it, and return the information to Divvy.

(Note: the specification of a content type has to occur on both sides. In Divvy, the content type needs to be checked on the configuration screen. In Episerver, a mapping needs to be present which maps the Divvy content type to an Episerver content type.)

When this occurs, Divvy will modify its UI for that content. In the Divvy UI, when Episerver-synchronized content is previwed, Divvy will make an API request to Episerver. Episerver will return a string of HTML to Divvy (from the `GetPreviewHtml` on the content object, from `IDivvyMappedType`) for Divvy to display. Additionally, the response from Episerver will contain the Edit Mode URL that Divvy will use when a user clicks the "Edit in Episerver" button.

When Divvy-synchronized content is published in Episerver, an asynchronous request will be sent to Divvy with the new status of the content object.

## Extension Points

The 'DivvyManager' class includes multiple events and one delegate for customization.

* `OnBeforeParseContentGatewayRequest` executes before the inbound JSON is parsed. The raw request body can be modified before parsing. To cancel Divvy processing, set `CancelAction` to `true`.
* `OnAfterParseContentGatewayRequest` executes after the inbound JSON is parsed. The JSON can be manipulated. To cancel Divvy processing, set `CancelAction` to `true`.
* `OnBeforeContentCreation` executes after mapping is completed, but immediately before content is created. To cancel Divvy processing, set `CancelAction` to `true`.
* `OnAfterGeneratePreviewHtml` executes after the preview HTML is generated, but before it is sent to Divvy. It can be modified as desired.
* `PreviewProvider` is a delegate that can be reassigned if a different method of generating HTML is desired.
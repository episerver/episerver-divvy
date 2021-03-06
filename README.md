﻿# Episerver Divvy Integration

This add-on allows a Divvy account to interact with an Episerver instance.

* When editors create content in their Divvy account, corresponding content objects will be created in the Episerver instance
* When previewing content, Divvy will retrieve preview HTML directly from Episerver
* For Episerver content, Divvy provides a "Edit in Episerver" button to link directly to the Episerver UI
* When content is published or deleted in Episerver, Divvy will be notified and will adjust the content status

## Installation

This add-on also requires enablement of your Divvy account.

To enable in Divvy:

1. Access "Integration Admin"
2. Select "Episerver" in the left menu (Divvy will need to enable this option for your account)
3. Enter the domain name of your Episerver website
4. Enter the auth token value from your web.config (this should be a 32-digit GUID; anything from http://givemeguid.com/ should be fine)
5. Select the content types which should be synchronized to Episerver content

To enable in Episerver:

1. Compile the source into your project.
2. In `/configuration/configSections` add the configuration element as shown below
3. in `/configuration` add the Divvy element as shown below
2. In the web.config, on the `divvy` element, add the same `authToken` from Step #4 above
2. In the web.config, add a "mapping" element for for Divvy content type that should result in the synchronization of an Episerver content object (see below)

### Config Section Element

```
<section name="divvy" type="Episerver.Labs.Divvy.Config.DivvyConfigSection"/>
```

### Divvy Element

```
<divvy enabled="true" debugRole="DivvyDebug" authToken="[the unique auth token]">
   <mapping divvyType="[name of content type in Divvy]" episerverType="[name of Episerver content type]" parent="[the numeric ID of where the content should be created]"/>
</divvy>
```

* **enabled:** set to false to disable the entire system without uninstalling (removing the config element will do the same thing)
* **debugRole:** authenticated users in this role will be able to view the debug API without passing an auth token; leave blank if not needed (see below for more information on the debug API)
* **authToken:** the token both Episerver and Divvy will use to communicate; this needs to be the same in both systems

For mappings:

* **divvyType:** the name of a Divvy content type that should be handled
* **episerverType:** the name of the Episerver type that should be created in response to the specified Divvy content type
* **parent:** the numeric ID of the content under which the Episerver content should be created

## Process

When the Episerver integration is enabled in Divvy, an API request is sent to Episerver _every time_ new content is created in Divvy of one of the content types specified on the configuration screen.

If the Episerver instance is not configured to handle the specific content type created, Episerver returns a `null` value to Divvy, and Divvy continues normally.

If the Episerver instance *is* configured to handle the specific content type created (via a `mapping` element, as shown above), the Episerver instance will create a draft object of the specified type, store the mapping between Divvy type and Episerver type, and return the information to Divvy.

(Note: the specification of a content type has to occur on both sides. In Divvy, the content type needs to be checked on the configuration screen. In Episerver, a mapping needs to be present which maps the Divvy content type to an Episerver content type.)

When this occurs, Divvy will modify its UI for that content. In the Divvy UI, when Episerver-synchronized content is previwed, Divvy will make an API request to Episerver. Episerver will return a string of HTML to Divvy to display. Additionally, the response from Episerver will contain the Edit Mode URL that Divvy will use when a user clicks the "Edit in Episerver" button.

When Divvy-synchronized content is published or deleted in Episerver, an asynchronous request (usually within 30 seconds) will be sent to Divvy with the new status of the content object.

## Extension Points

The `DivvyManager` class includes multiple events and one delegate for customization.

* `OnBeforeParseContentGatewayRequest` executes before the inbound JSON is parsed. The raw request body string can be modified before parsing. To cancel Divvy processing, set `CancelAction` to `true`.
* `OnAfterParseContentGatewayRequest` executes after the inbound JSON is parsed. The JSON can be manipulated. To cancel Divvy processing, set `CancelAction` to `true`.
* `OnBeforeContentCreation` executes after mapping is completed, but immediately before content is created. To cancel Divvy processing, set `CancelAction` to `true`. To change where the Episerver content is created, modify `e.IntendedParent`. To change the content type of content to be created, modify `e.IntendedTypeName`.
* `OnAfterGeneratePreviewHtml` executes after the preview HTML is generated, but before it is sent to Divvy. Modify `e.PreviewHtml` as desired.
* `PreviewProvider` is a delegate that can be reassigned if a different method of generating HTML is desired. It's expected that this will be modified for each installation.

## Debugging API

There's a limited REST API to retrieve debugging info from a running instance of the integration.

You can authenticate two ways:

1. Be logged into Episerver under an account which is a member of the `debugRole` as defined in the config
2. Pass the authorization token in the `auth` querystring argument: `/divvy/debug/log?auth=[token]`

Debug logging is off by default at application start. It must be actively turned on. It should be turned off (and cleared) when not needed.

**/divvy/debug/log/status**

Displays whether logging is off or on.

**/divvy/debug/log/on**

Turns request debug logging on.

**/divvy/debug/log**

Returns the general log. Each inbound request will be assigned a GUID as its `requestKey`.

**/divvy/debug/log/[requestKey]**

Returns the log entries for that the specified `requestKey`.

**/divvy/debug/log/off**

Turns logging off and clears all log entries.

**/divvy/debug/types**

Returns the mapped types.

**/divvy/debug/mappings**

Returns all content mappings -- links between Divvy content and Episerver content.

**/divvy/debug/mappings/clear**

Deletes all mappings. _Use with care._

**/divvy/debug/mappings/clear/[id]**

Deletes a specific mapping.
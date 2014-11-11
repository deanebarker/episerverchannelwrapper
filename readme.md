# EPiServer Channel Wrapper

The EPiServer Channel Wrapper is a lightweight wrapper around the EPiServer content channel architecture.  It's designed to make it as simple as possible to push internal data into EPiServer and keep it updated over time.

The use case here is a scheduled job that continually pushes external data into EPiServer, ensuring that it's up-to-date.

>**Example:**    
>A university manages its class descriptions in a SQL-based ERP system. Using the EPiServer Channel Wrapper, they create a command line executable which retrieves all the course descriptions and syncs them against pages in EPiServer. When new courses are created, they are created in EPiServer. When existing courses are updated, they are updated in EPiServer.

(See below for information on deletes.)

The following code opens a channel:

    var channel = new EPiServerChannel(
      "ChannelName",
      "http://siteurl.com",
      "username",
      "password"
    );

Then you can throw virtually anything at its "Process" method:

    channel.Process(myDataRow);
    channel.Process(myDictionaryObject);
    channel.Process(myPOCO);
    channel.Process(myAnonymousObject);

Internally, all of those items are converted to a Dictionary<string, object> before being applied against the content channel web service. (The objects are reflected with the property names becoming dictionary keys.)

The resulting dictionary must have these two keys:

1. PageName
2. ExternalId

**PageName** is obvious -- the key that represents the value that should be the page name.

**ExternalId** is the key corresponding to the unique identifier of the object *outside EPiServer*. It can be whatever you want, but it needs to be unique and stable.  (A database key, for instance, or a filename/path that won't change.)

The Channel Wrapper will keep a mapping between this external ID and the corresponding PageGuid of that item inside EPiServer. This is how it avoids duplicates -- if it has a mapping between the external ID and a PageGuid, it will transmit the Guid, thus telling EPiServer to *update* a page, rather than create a new one.

These mappings are abstracted to a "Record Manager" class.  The default Record Manager stores its information in a text file. Other examples provided are for a SQL database and EPiServer's own DDS (for instances when you're pushing from one EPiServer installation to another). The IRecordManager interface is available for you to implement your own.

Examples are provided showing importing from:

1. Code
2. A set of XML files
3. A SQL database (actually SQL CE)
4. Another EPiServer installation (so, very similar to content mirroring)

## Deletions

There is prototype code for deletions ("ProcessDeletions"), but it's flaky yet. The problem is that the only way to determine if something has been deleted is to compare all the available keys from the external database (A) against the key mapping (B), and delete everything that appears in B but not A.

This hasn't been tested yet, and the Record Manager interface needs to be extended to enable this. *Coming soon.* 
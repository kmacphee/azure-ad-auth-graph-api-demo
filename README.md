# ToDoGraphDemo

This is a demo MVC app that I wrote to teach myself how to authenticate with Azure AD, acquire an access token and use that token to access Azure AD-protected resources.

It's a simple "To Do List" app that synchronizes with a OneNote page via Graph API, so if you want to try it out for yourself you will need the appropriate Office 365 account linked to your Azure AD tenant. Identity in Office 365 is managed by AzureAD so it was an easy choice for experimenting with this.

Broadly, the code does the following:

1. Configures OWIN to authenticate via OpenID Connect against the Azure AD v2 endpoint, for a given tenant.
1. On user sign-in, acquires an access token for the user via Authorization Code Flow, then instantiates an MSAL token cache for the user session to store the token.
1. Authenticates a GraphServiceClient (an OData client from Microsoft for Graph API) with the access token, for synchronization with OneNote.

#### Room for improvement

The UI isn't much to write home about, I just wanted something basic (but functional) for testing the app. It wasn't my main focus whilst putting this together. It's mainly Razor forms linked to Controller methods. Because of this, most user interaction is done with POST methods. It would be better if the Web API were properly RESTful.

## Supporting Blog Posts

- https://anchorloop.com/

## Application Registration

For a user to authenticate with Azure AD you will need to register the app in the [Application Registration Portal](https://apps.dev.microsoft.com) and set the appropriate config variables in `Web.config`.

```xml
<add key="ClientId" value="add-your-client-id" />
<add key="ClientSecret" value="add-your-client-secret" />
<add key="RedirectUri" value="add-your-redirect-uri" />
<add key="Tenant" value="add-your-tenant" />
```

- `ClientId` should be set to the `Application Id` of your app in the registration portal.
- `ClientSecret` is an application secret (password) that you can generate in the portal with the `Generate New Password` button.
- `RedirectUri` is the address that OpenId Connect will redirect to post-authentication.
- `Tenant` is the domain of the Azure AD tenant whose users are allowed to log in to the app, e.g. contoso.com.

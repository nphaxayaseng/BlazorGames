# Deploying a .NET Core Blazor App to Netlify using GitHub Actions

### Prerequisites and a Special Note

This tutorial assumes the following:

1. You already have a Blazor WebAssembly repository in GitHub.
2. You already have a Netlify connected to the GitHub repository.

### Disable Netlify Builds

The first thing we need to do is disable Netlify's automatic builds. Since Netlify cannot build .NET Core applications, they are of no use to us, so we need to get them out of the way.

In the Netlify site for your app, go to Build and Deploy, click Edit Settings, and then select Stop Builds:

![img](https://exceptionnotfound.net/content/images/2020/06/image-10.png)

### Get Your Personal Access Token and Site API ID

You need to generate two tokens from Netlify in order to allow GitHub actions to deploy your site: a personal access token and a site API ID.

The personal access token can be generated in Netlify by clicking on your account, selecting Applications, then, under Personal Access Tokens, selecting "New access token". This should put you on a screen that looks like this:

![img](https://exceptionnotfound.net/content/images/2020/06/image-11.png)You can name this token whatever you want; I called it NETLIFY_AUTH_TOKEN. 

The site API ID is found under the Site Settings in Netlify:

![img](https://exceptionnotfound.net/content/images/2020/06/image-16.png)

Both of these need to be created as Secrets in your GitHub repository. Go to the repository page, then click Settings, and then Secrets.

![img](https://exceptionnotfound.net/content/images/2020/06/image-12.png)Once these secrets are created, you can never view them again.

OK! We've gathered all the necessary things to make our build in GitHub Actions, so let's get started on that!

### Using GitHub Actions

In your GitHub repository, go to the Actions tab, and click New Workflow.

On the next page, scroll down to More Continuous Integration Workflows, click it, and then select .NET Core.

![img](https://exceptionnotfound.net/content/images/2020/06/image-17.png)

![img](https://exceptionnotfound.net/content/images/2020/06/image-18.png)

The next page is where we set up our YAML file to build our .NET Core application and deploy it to Netlify. Here's the annotated YAML file that's in use in my repository.

```yaml
name: BlazorGames # Name of the workflow.
on: [push] # Action on which the workflow runs. Can be push, pull_request, page_build, or many others

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@master # Checkout the master branch
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1 # Setup .NET Core
      with:
        dotnet-version: 3.1.300 # Change to your version of .NET Core
    - name: Build with dotnet
      run: dotnet build --configuration Release 
    - name: Publish Blazor webassembly using dotnet 
      #create Blazor WebAssembly dist output folder in the project directory
      run: dotnet publish -c Release --no-build -o publishoutput # Don't build again, just publish
    - name: Publish generated Blazor webassembly to Netlify
      uses: netlify/actions/cli@master #uses Netlify Cli actions
      env: # These are the environment variables added in GitHub Secrets for this repo
          NETLIFY_AUTH_TOKEN: ${{ secrets.NETLIFY_AUTH_TOKEN }}
          NETLIFY_SITE_ID: ${{ secrets.NETLIFY_SITE_ID }}
      with:
          args: deploy --dir=publishoutput/wwwroot --prod #push this folder to Netlify
          secrets: '["NETLIFY_AUTH_TOKEN", "NETLIFY_SITE_ID"]' 
```

The key parts of this file are that it:

1. Builds the site in .NET Core first, and then
2. Publishes the site to Netlify using the auth token and site ID secrets from earlier.

Run this action, and you should see that your site is now deployed to Netlify!

### Summary

In order to deploy a .NET Core Blazor app to Netlify, we can use GitHub actions. We need a personal auth token and site API ID included in our GitHub repo as secrets, and we need a YAML file to run the build and publish the site; the specific YAML file I am using is given above. Don't forget to disable Netlify's automatic builds!
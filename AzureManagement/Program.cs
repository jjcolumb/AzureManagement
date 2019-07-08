using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Search.Fluent.Models;
using System.Threading;
using System.Net.Http.Headers;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.IO;

namespace AzureManagement
{
    class Program
    {


        /*Retrying role assignment creation: 2/36
            {
              "appId": "cd5b6047-85dd-403a-8c65-0385f1a4a794",
              "displayName": "azure-cli-2019-07-08-04-08-29",
              "name": "http://azure-cli-2019-07-08-04-08-29",
              "password": "b3cedfec-6a60-4eca-b041-5a2717c75b24",
              "tenant": "f7fc9502-dd24-4a98-bb1e-3401fcf5d184"
            }
         */

        private static readonly string clientId = "cd5b6047-85dd-403a-8c65-0385f1a4a794";
        private static readonly string clientSecret = "b3cedfec-6a60-4eca-b041-5a2717c75b24";
        private static readonly string tenantId = "f7fc9502-dd24-4a98-bb1e-3401fcf5d184";
        static void Main(string[] args)
        {

            try
            {
                //=================================================================
                // Authenticate
                //var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);


                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                // Print selected subscription
                Utilities.Log("Selected subscription: " + azure.SubscriptionId);

                RunSample1(azure);
               // RunSample2(azure);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }


        /**
        * Azure App Service basic sample for managing web apps.
        *  - Create 3 web apps under the same new app service plan:
        *    - 1, 2 are in the same resource group, 3 in a different one
        *    - Stop and start 1, restart 2
        *    - Add Java support to app 3
        *  - List web apps
        *  - Delete a web app
        */

        public static void RunSample1(IAzure azure)
        {
            string app1Name = SdkContext.RandomResourceName("webapp1-", 20);
            string app2Name = SdkContext.RandomResourceName("webapp2-", 20);
            string app3Name = SdkContext.RandomResourceName("webapp3-", 20);
            string rg1Name = SdkContext.RandomResourceName("rg1NEMV_", 24);
            string rg2Name = SdkContext.RandomResourceName("rg2NEMV_", 24);

            try
            {
                //============================================================
                // Create a web app with a new app service plan

                Utilities.Log("Creating web app " + app1Name + " in resource group " + rg1Name + "...");

                var app1 = azure.WebApps
                        .Define(app1Name)
                        .WithRegion(Region.USWest)
                        .WithNewResourceGroup(rg1Name)
                        .WithNewWindowsPlan(PricingTier.StandardS1)
                        .Create();

                Utilities.Log("Created web app " + app1.Name);
                Utilities.Print(app1);

                //============================================================
                // Create a second web app with the same app service plan

                Utilities.Log("Creating another web app " + app2Name + " in resource group " + rg1Name + "...");
                var plan = azure.AppServices.AppServicePlans.GetById(app1.AppServicePlanId);
                var app2 = azure.WebApps
                        .Define(app2Name)
                        .WithExistingWindowsPlan(plan)
                        .WithExistingResourceGroup(rg1Name)
                        .Create();

                Utilities.Log("Created web app " + app2.Name);
                Utilities.Print(app2);

                //============================================================
                // Create a third web app with the same app service plan, but
                // in a different resource group

                Utilities.Log("Creating another web app " + app3Name + " in resource group " + rg2Name + "...");
                var app3 = azure.WebApps
                        .Define(app3Name)
                        .WithExistingWindowsPlan(plan)
                        .WithNewResourceGroup(rg2Name)
                        .Create();

                Utilities.Log("Created web app " + app3.Name);
                Utilities.Print(app3);

                //============================================================
                // stop and start app1, restart app 2
                Utilities.Log("Stopping web app " + app1.Name);
                app1.Stop();
                Utilities.Log("Stopped web app " + app1.Name);
                Utilities.Print(app1);
                Utilities.Log("Starting web app " + app1.Name);
                app1.Start();
                Utilities.Log("Started web app " + app1.Name);
                Utilities.Print(app1);
                Utilities.Log("Restarting web app " + app2.Name);
                app2.Restart();
                Utilities.Log("Restarted web app " + app2.Name);
                Utilities.Print(app2);

                
                // List web apps

                Utilities.Log("Printing list of web apps in resource group " + rg1Name + "...");

                foreach (var webApp in azure.WebApps.ListByResourceGroup(rg1Name))
                {
                    Utilities.Print(webApp);
                }

                Utilities.Log("Printing list of web apps in resource group " + rg2Name + "...");

                foreach (var webApp in azure.WebApps.ListByResourceGroup(rg2Name))
                {
                    Utilities.Print(webApp);
                }

                //=============================================================
                // Delete a web app

                Utilities.Log("Deleting web app " + app1Name + "...");
                azure.WebApps.DeleteByResourceGroup(rg1Name, app1Name);
                Utilities.Log("Deleted web app " + app1Name + "...");

                Utilities.Log("Printing list of web apps in resource group " + rg1Name + " again...");
                foreach (var webApp in azure.WebApps.ListByResourceGroup(rg1Name))
                {
                    Utilities.Print(webApp);
                }
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rg2Name);
                    azure.ResourceGroups.DeleteByName(rg2Name);
                    Utilities.Log("Deleted Resource Group: " + rg2Name);
                    Utilities.Log("Deleting Resource Group: " + rg1Name);
                    azure.ResourceGroups.DeleteByName(rg1Name);
                    Utilities.Log("Deleted Resource Group: " + rg1Name);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }


        private const string Suffix = ".azurewebsites.net";

        /**
         * Azure App Service basic sample for managing web apps.
         * Note: you need to have the Git command line available on your PATH. The sample makes a direct call to 'git'.
         *  - Create 5 web apps under the same new app service plan:
         *    - Deploy to 1 using FTP
         *    - Deploy to 2 using local Git repository
         *    - Deploy to 3 using a publicly available Git repository
         *    - Deploy to 4 using a GitHub repository with continuous integration
         *    - Deploy to 5 using Web Deploy
         */
        public static void RunSample2(IAzure azure)
        {
            string app1Name = SdkContext.RandomResourceName("webapp1-", 20);
            string app2Name = SdkContext.RandomResourceName("webapp2-", 20);
            string app3Name = SdkContext.RandomResourceName("webapp3-", 20);
            string app4Name = SdkContext.RandomResourceName("webapp4-", 20);
            string app5Name = SdkContext.RandomResourceName("webapp5-", 20);
            string app1Url = app1Name + Suffix;
            string app2Url = app2Name + Suffix;
            string app3Url = app3Name + Suffix;
            string app4Url = app4Name + Suffix;
            string app5Url = app5Name + Suffix;
            string rgName = SdkContext.RandomResourceName("rg1NEMV_", 24);

            try
            {
                //============================================================
                // Create a web app with a new app service plan

                Utilities.Log("Creating web app " + app1Name + " in resource group " + rgName + "...");

                var app1 = azure.WebApps
                        .Define(app1Name)
                        .WithRegion(Region.USWest)
                        .WithNewResourceGroup(rgName)
                        .WithNewWindowsPlan(PricingTier.StandardS1)
                        .WithJavaVersion(JavaVersion.V8Newest)
                        .WithWebContainer(WebContainer.Tomcat8_0Newest)
                        .Create();

                Utilities.Log("Created web app " + app1.Name);
                Utilities.Print(app1);

                //============================================================
                // Deploy to app 1 through FTP

                Utilities.Log("Deploying helloworld.War to " + app1Name + " through FTP...");

                Utilities.UploadFileToWebApp(
                    app1.GetPublishingProfile(),
                    Path.Combine(Utilities.ProjectPath, "Asset", "helloworld.war"));

                Utilities.Log("Deployment helloworld.War to web app " + app1.Name + " completed");
                Utilities.Print(app1);

                // warm up
                Utilities.Log("Warming up " + app1Url + "/helloworld...");
                Utilities.CheckAddress("http://" + app1Url + "/helloworld");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app1Url + "/helloworld...");
                Utilities.Log(Utilities.CheckAddress("http://" + app1Url + "/helloworld"));

                //============================================================
                // Create a second web app with local git source control

                Utilities.Log("Creating another web app " + app2Name + " in resource group " + rgName + "...");
                var plan = azure.AppServices.AppServicePlans.GetById(app1.AppServicePlanId);
                var app2 = azure.WebApps
                        .Define(app2Name)
                        .WithExistingWindowsPlan(plan)
                        .WithExistingResourceGroup(rgName)
                        .WithLocalGitSourceControl()
                        .WithJavaVersion(JavaVersion.V8Newest)
                        .WithWebContainer(WebContainer.Tomcat8_0Newest)
                        .Create();

                Utilities.Log("Created web app " + app2.Name);
                Utilities.Print(app2);

                //============================================================
                // Deploy to app 2 through local Git

                Utilities.Log("Deploying a local Tomcat source to " + app2Name + " through Git...");

                var profile = app2.GetPublishingProfile();
                Utilities.DeployByGit(profile, "azure-samples-appservice-helloworld");

                Utilities.Log("Deployment to web app " + app2.Name + " completed");
                Utilities.Print(app2);

                // warm up
                Utilities.Log("Warming up " + app2Url + "/helloworld...");
                Utilities.CheckAddress("http://" + app2Url + "/helloworld");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app2Url + "/helloworld...");
                Utilities.Log(Utilities.CheckAddress("http://" + app2Url + "/helloworld"));

                //============================================================
                // Create a 3rd web app with a public GitHub repo in Azure-Samples

                Utilities.Log("Creating another web app " + app3Name + "...");
                var app3 = azure.WebApps
                        .Define(app3Name)
                        .WithExistingWindowsPlan(plan)
                        .WithNewResourceGroup(rgName)
                        .DefineSourceControl()
                            .WithPublicGitRepository("https://github.com/Azure-Samples/app-service-web-dotnet-get-started")
                            .WithBranch("master")
                            .Attach()
                        .Create();

                Utilities.Log("Created web app " + app3.Name);
                Utilities.Print(app3);

                // warm up
                Utilities.Log("Warming up " + app3Url + "...");
                Utilities.CheckAddress("http://" + app3Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app3Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app3Url));

                //============================================================
                // Create a 4th web app with a personal GitHub repo and turn on continuous integration

                Utilities.Log("Creating another web app " + app4Name + "...");
                var app4 = azure.WebApps
                        .Define(app4Name)
                        .WithExistingWindowsPlan(plan)
                        .WithExistingResourceGroup(rgName)
                        // Uncomment the following lines to turn on 4th scenario
                        //.DefineSourceControl()
                        //    .WithContinuouslyIntegratedGitHubRepository("username", "reponame")
                        //    .WithBranch("master")
                        //    .WithGitHubAccessToken("YOUR GITHUB PERSONAL TOKEN")
                        //    .Attach()
                        .Create();

                Utilities.Log("Created web app " + app4.Name);
                Utilities.Print(app4);

                // warm up
                Utilities.Log("Warming up " + app4Url + "...");
                Utilities.CheckAddress("http://" + app4Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app4Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app4Url));

                //============================================================
                // Create a 5th web app with web deploy

                Utilities.Log("Creating another web app " + app5Name + "...");

                IWebApp app5 = azure.WebApps.Define(app5Name)
                    .WithExistingWindowsPlan(plan)
                    .WithExistingResourceGroup(rgName)
                    .WithNetFrameworkVersion(NetFrameworkVersion.V4_6)
                    .Create();

                Utilities.Log("Created web app " + app5.Name);
                Utilities.Print(app5);

                //============================================================
                // Deploy to the 5th web app through web deploy

                Utilities.Log("Deploying a bakery website to " + app5Name + " through web deploy...");

                app5.Deploy()
                    .WithPackageUri("https://github.com/Azure/azure-libraries-for-net/raw/master/Tests/Fluent.Tests/Assets/bakery-webapp.zip")
                    .WithExistingDeploymentsDeleted(true)
                    .Execute();

                Utilities.Log("Deployment to web app " + app5Name + " completed.");
                Utilities.Print(app5);

                // warm up
                Utilities.Log("Warming up " + app5Url + "...");
                Utilities.CheckAddress("http://" + app5Url);
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app5Url + "...");
                Utilities.Log(Utilities.CheckAddress("http://" + app5Url));
            }
            catch (FileNotFoundException)
            {
                Utilities.Log("Cannot find 'git' command line. Make sure Git is installed and the directory of git.exe is included in your PATH environment variable.");
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.DeleteByName(rgName);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

       
    }

}


---
Type: Sample
Languages:
- CSharp PL, Linux Bash Script
Products:
- Azure Batch, Kubernetes, TESK
Problem Definition: "Customers wanting to run Genomics sequencing workloads on a public cloud (Azure, AWS, GCP) often want to automate the process of both deploying a stand-alone Kubernetes cluster and deploying a GA4GH compliant TES engine on the cluster.  The steps involved in running a TES engine on a Kubernetes cluster running on a high capacity VM is often laborious, challenging and error prone.  This project provides a simple automated solution and address several problems tied to this use case.  Most importantly, this GitHub project details the steps for provisioning a Kubernetes cluster on Azure Batch Service, a job scheduling and compute management platform that enables running large scale parallel and HPC applications efficiently in the cloud."
UrlFragment: azure-batch-k8s-jobs
---

# A solution framework for running GA4GH TES Engine on Kubernetes on Azure Batch Service

This project provides a skeleton framework/API for deploying TES application workloads on Kubernetes running on Azure Batch Service.  The framework provides a foundational API layer which customers can easily extend and build upon to meet their unique needs and requirements. 

## Prerequisites

- Azure CLI (2.7+) installed on your workstation/VM
- Azure Container Registry (ACR) or access to Docker Hub
- Visual Studio 2019, or [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) for Linux, macOS, or Windows installed on your workstation/VM

## Resources

- [Docker documentation](https://docs.docker.com/)
- [Kubernetes documentation](https://kubernetes.io/docs/home/)
- [Azure Linux VM documentation](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/)
- [Azure Batch Service documentation](https://docs.microsoft.com/en-us/azure/batch/)
- [Azure Batch Service .NET Core SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/batch/client?view=azure-dotnet)
- [Azure Storage documentation](https://docs.microsoft.com/en-us/azure/storage/)
- [Azure Storage .NET Core SDK v12](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme?view=azure-dotnet)

**Important Notes:**
- Within all command snippets, a command preceded by a hash ('#') symbol denotes a comment.
- For Azure CLI commands, provide values for all command parameters enclosed within angle brackets ('<'...'>').

## A. Create and store a custom Linux VM image in an Azure Shared Image Gallery

Follow the steps below.

1. Provision a Linux VM on Azure

   Use one of the Azure marketplace Linux images and provision a VM. Refer to the docs [here](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/cli-ps-findimage) to view the available Linux images.  Then follow the steps [here](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/tutorial-manage-vm) to provision a VM on Azure.

   >>**NOTE:** The scripts included in this project have been tested to work with Ubuntu/Debian Linux.  You may need to tweak the shell scripts / commands if you decide to use a different Linux flavor.

2. Install Docker container engine on the Linux VM

   Login to the Linux VM via SSH.  Follow the instructions [here](https://docs.docker.com/engine/install/ubuntu/) to install **docker-ce** container engine on the VM.  Then configure [docker to start on boot](https://docs.docker.com/engine/install/linux-postinstall/#configure-docker-to-start-on-boot).

3. Deprovision, deallocate and generalize the Linux VM

   Refer to the command snippet below to **deprovision** the VM.
   ```
   # Deprovision the VM.
   $ sudo waagent -deprovision+user
   #
   ```
   Log out of the VM.

   Refer to the command snippet below to **deallocate** and **generalize** the VM.
   ```
   # Remember to substitute appropriate values for Azure Resource Group and VM Name.
   #
   # Deallocate the VM.  This command deallocates and shuts the VM down.
   $ az vm deallocate -g <resource-group-name> -n <linux-vm-name>
   #
   # Generalize the VM.  VM cannot be started after running this command.
   $ az vm generalize -g <resource-group-name> -n <linux-vm-name>
   #
   ```

4. Create a shared image gallery, image definition and image version

   >>**NOTE:** The Azure resources created in this step can also be provisioned via the Azure Portal.

   Refer to the command snippet below.
   ```
   # Remember to substitute appropriate values for 
   # - Azure Resource Group
   # - Image publisher name
   # - Target regions for image version
   # - Replica counts for image version
   #
   # Create a shared image gallery.
   $ az sig create -g <resource-group-name> --gallery-name k8sGallery
   #
   # Create a shared image definition.
   $ az sig image-definition create \
       --resource-group <resource-group-name> \
       --gallery-name k8sGallery \
       --gallery-image-definition k8sMasterImage \
       --publisher <publisher-name> \
       --offer k8sBatch \
       --sku 1.0 \
       --os-type Linux \
       --os-state generalized
   #
   # Save the image ID in an env variable
   $ ImageID=$(az image list --query "[].[id]" -o tsv)
   #
   # Create the image version
   $ az sig image-version create \
       --resource-group <resource-group-name> \
       --gallery-name k8sGallery \
       --gallery-image-definition k8sMasterImage \
       --gallery-image-version 2020.10.1 \
       --target-regions "westus2=1" \
       --replica-count 1 \
       --managed-image $ImageID
   #
   ```

   ```
   # (Optional) Build the application.  Make sure there are no compilation errors or warnings.
   $ dotnet build
   #
   # Run the container build
   # Substitute correct values for the following:
   #   acr-name : Name of your ACR instance
   $ docker build -t <acr-name>.azurecr.io/recycle-vmss:latest
   #
   # List the container images on your workstation/VM. The image built in the preceding step should be listed.
   $ docker images
   #
   # Before you can login to ACR, you should be logged into Azure via Azure CLI.
   # Login to the ACR instance. Substitute the correct value for 'acr-name' in the command below.
   $ az acr login -n <acr-name>
   #
   # Push the container image into ACR.  Substitute correct value for 'acr-name'.
   $ docker push <acr-name>.azurecr.io/recycle-vmss:latest
   #
   # Verify the image was pushed into your ACR instance via Azure Portal or by running the CLI command below.
   $ az acr repository list -n <acr-name> -o table
   #
   ```

## B. Create Azure Batch Account and Service Principal


   Environment Variable Name | Value | Description
   ------------------------- | ----- | -----------
   AzureWebJobsStorage | Required | Azure Storage Account connection string. Azure Functions runtime uses this storage account to store/manipulate WebJobs logs. 
   WEBSITE_TIME_ZONE | Required | Set this value based on your time zone. The list of TZ strings can be found [here](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).
   FUNCTIONS_WORKER_RUNTIME | dotnet | Runtime for this Azure Function is **dotnet**. Leave this value as is.
   AZURE_AD_TENANT_ID | Required | Specify the Azure AD Tenant ID.  You can use Azure Portal or CLI to retrieve this value.
   AZURE_AD_TOKEN_EP | https://login.microsoftonline.com | Azure AD Token end-point.  Leave this value as is.
   AZURE_SP_CLIENT_ID | Required | This solution uses OAuth Client Credentials Flow to authenticate a *Service Principal* against Azure AD.  An Azure Service Principal ID with appropriate permissions to access the target VMSS resource is required.  Refer to [Azure AD documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal) to create a Service Principal and assign it relevant permissions. 
   AZURE_SP_CLIENT_SECRET | Required | The secret associated with the Azure AD Service Principal.
   AZURE_SP_APP_ID_URI | https://management.azure.com | This is the Azure Resource Manager API end-point.  Leave this value as is.
   AZURE_SUBSCRIPTION_ID | Required | Specify the Azure Subscription ID where the AKS cluster VMSS resource is deployed.
   AZURE_RES_GROUP_NAME | Required | Specify the Resource Group in which the VMSS resource is deployed.  This is usually the AKS cluster **node** resource group.
   AZURE_VMSS_NAME | Required | Specify the name of the VMSS.
   AZURE_VMSS_API_VER | 2019-12-01 | This is the Azure resource provider API version. You can leave this value as is.
   AZURE_VMSS_ACTION | Required | Specify the **action** to be performed on the VMSS.  Supported values are - **start** (to start the VMSS), **deallocate** (to stop and deallocate the VM's associated with the VMSS).
   VmssTriggerSchedule | Required | An NCRONTAB expression which specifies the Function execution schedule. Refer to the [Function documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions) for details and correct syntax.

## Project code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
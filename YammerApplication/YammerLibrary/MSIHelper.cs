// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Threading.Tasks;
using System.Management.Automation;
using Microsoft.Azure.KeyVault.Models;


namespace YammerLibrary
{
    [Cmdlet(VerbsCommon.Get, "Secret")]
    public class GetSecretCmdlet : PSCmdlet
    {
        private string _secretURI;
        private string _queueName;
        private string secret;

        [Parameter(Mandatory = true, HelpMessage = "The URI of the secret in the Key Vault")]
        [ValidateNotNullOrEmpty]
        public string secretURI
        {
            get { return _secretURI; }
            set { _secretURI = value; }
        }
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            secret = GetSecretAsync().Result;
            string valueSecret = "The secret value is: " + secret;
        }

        protected override void StopProcessing()
        {
            base.StopProcessing();
        }

        public async Task<string> GetSecretAsync()
        {

            AzureServiceTokenProvider azureServiceTokenProvider =
                            new AzureServiceTokenProvider();

            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            string secretUriSettings = "";

            SecretBundle secret;

            if (!String.IsNullOrEmpty(secretUriSettings))
            {
                secret = await kv.GetSecretAsync(secretUriSettings).ConfigureAwait(false);
            }
            else
            {
                secret = await kv.GetSecretAsync(secretURI).ConfigureAwait(false);
            }

            return secret.Value;

        }

    

    }
}

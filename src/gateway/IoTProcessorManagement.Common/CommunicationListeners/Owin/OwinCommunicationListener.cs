﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IoTProcessorManagement.Common
{
    using System;
    using System.Fabric;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin.Hosting;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    // generic Owin Listener
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly IOwinListenerSpec pipelineSpec;
        private readonly ServiceContext serviceContext;
        private IDisposable webServer = null;
        private string listeningAddress = string.Empty;
        private string publishingAddress = string.Empty;
        private string mHostAddress = null;

        public OwinCommunicationListener(IOwinListenerSpec pipelineSpec, ServiceContext serviceContext, string publishingAddressHostName)
            : this(pipelineSpec, serviceContext)
        {
            this.mHostAddress = publishingAddressHostName;
        }

        public OwinCommunicationListener(IOwinListenerSpec pipelineSpec, ServiceContext serviceContext)
        {
            this.pipelineSpec = pipelineSpec;
            this.serviceContext = serviceContext;
        }

        public void Abort()
        {
            if (null != this.webServer)
            {
                this.webServer.Dispose();
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.Abort();
            return Task.FromResult(true);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            StatefulServiceContext statefulInitParam = this.serviceContext as StatefulServiceContext;
            int port = this.serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndPoint").Port;


            if (statefulInitParam != null)
            {
                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}/{2}/{3}/",
                    port,
                    statefulInitParam.PartitionId,
                    statefulInitParam.ReplicaId,
                    Guid.NewGuid());
            }
            else
            {
                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/",
                    port);
            }

            this.publishingAddress = this.listeningAddress.Replace(
                "+",
                string.IsNullOrEmpty(this.mHostAddress) ? FabricRuntime.GetNodeContext().IPAddressOrFQDN : this.mHostAddress);
            this.webServer = WebApp.Start(this.listeningAddress, app => this.pipelineSpec.CreateOwinPipeline(app));

            return Task.FromResult(this.publishingAddress);
        }
    }
}
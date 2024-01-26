# Platform MSs as great examples
The platform's own microservices are the best examples of how to write and run .NET microservices on top of Liquid Application Framework.

With a Microsoft CosmosDB emulator, an Azure ServiceBus instance, and an Azure SignalR service instance, you can run all 4 microservices (Profiles, Notifications, Reactivehub, and Scheduler) locally in your DEV machine.

Once built, they can be easily deployed as Kubernetes pods using Azure AKS.
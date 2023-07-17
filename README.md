# AVEVA Data Hub StreamingUpdates Service .NET REST API Sample

## Version
Developed against DotNet 6.0

## Requirements
The .NET Core CLI is referenced in this sample and should be installed in order to run the sample from command line.

## Definitions
* ADH: AVEVA DataHub
* SDS: Sequential Data Store

## About this repository

The sample code in this repository demonstrates REST API calls to ADH for creating a Signup to receive resource updates. Signups allow clients to subscribe resources (for example, streams) and get updates for those resources.

The sample will perform the following procedures:
1. Obtain an OAuth token for ADH using a client-credentials client.
2. Create a simple SDS Type.
3. Create a SDS Stream.
4. Create an ADH Signup against the created resources (streams).
5. Make an API request to GetSignup to activate the signup.
6. Make updates to the Streams (post data to stream).
7. Make an API request to GetUpdates and ensure that data updates are received.
8. Create a new SDS Stream and update Signup resources to include the new stream.
9. Make an API request to GetSignup to view signup with updated resources.
10. Make an API request to GetUpdates and ensure that data updates received.

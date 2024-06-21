# OPC-UA_Client
An easier way to access an OPC-UA Server. This acts as an Add-On for the standard OPC-UA Library, not a completely new one.
# Installation
- Add a reference to
  - Microsoft.Extensions.Logging.Abstractions.dll
  - Newtonsoft.Json.dll
  - Opc.Ua.Client.dll
  - Opc.Ua.Configuration.dll
  - Opc.Ua.Core.dll
  - Opc.Ua.Security.Certificates.dll
  - System.Diagnostics.DiagnosticSource.dll
  - System.Formats.Asn1.dll
  - System.Runtime.CompilerServices.Unsafe.dll
  - System.Security.Cryptography.Cng.dll
  - OPC-UA_Client.dll
- If you have the OPC-UA standard lib installed, you just need to reference the last dll
- Add a using directive: ``using Opc.Ua.CustomClient;``
# Library Content
## Client
- ``Client(string serverEndpoint)``: Creates a Client Instance that automatically connects itself to the specified OPC-UA Server.
- ``ServerObject GetObjectByName(string name)``: Gets a ServerObject by its Name.
- ``ServerObject GetObjectByNodeId(string nodeId)``: Gets a ServerObject by its Node Id.
- ``ServerObject[] GetObjects()``: Gets all ServerObjects.
- ``ServerObject[] GetObjects(int typeIdFilter)``: Gets all ServerObjects that have the specified Type Id.
- ``Session GetSession()``: Gets the current OPC-UA Session for accessing the Client with the standard OPC-UA Library.
- ``bool IsConnected()``: Returns true if the Client is currently connected to the OPC-UA Server.
- ``bool NameExists(string name)``: Returns true if an Object with the specified Name exists.
- ``bool NodeIdExists(string nodeId)``: Returns true if an Object with the specified Node Id exists.
- ``bool NameExistsInObject(string name, ServerObject obj)``: Returns true if an Object with the specified Name exists in the ServerObject.
- ``bool NodeIdExistsInObject(string nodeId, ServerObject obj)``: Returns true if an Object with the specified Node Id exists in the ServerObject.
- ``void Refresh()``: Refreshes the Connection to the OPC-UA Server. This should be used after creating new Objects with the Client.
- ``void RefreshObjects()``: Refreshes the Objects in the Client without refreshing the Connection.
- ``void RefreshChildObjects(ServerObject obj)``: Refreshes the Child Objects of the specified Object.
## ServerObject
- ``ReferenceDescription GetHandle()``: Gets the ReferenceDescription of the Object to access it with the standard OPC-UA Library.
- ``int GetTypeId()``: Gets the Type of the Object as an Integer. The Type Ids can be found in the Model.csv File of the OPC-UA Server.
- ``ServerObject GetParent()``: Gets the Parent Object that contains the current one.
- ``string GetNodeId()``: Gets the Node Id of the current Object.
- ``MethodDescription Method(string name)``: Gets the MethodDescription of the Method with the specified Name to call it.
- ``VariableDescription Variable(string name)``: Gets the VariableDescription of the Variable with the specified Name to get or set its value.
## MethodDescription
- ``MethodDescription(ReferenceDescription desc, ServerObject parent, Session session)``: Converts a ReferenceDescription to a MethodDescription.
- ``IList<object> Call(params object[] args)``: Calls the Method with the specified Arguments and returns one or multiple return Values.
## VariableDescription
- ``VariableDescription(ReferenceDescription desc, Session session)``: Converts a ReferenceDescription to a VariableDescription.
- ``T Get<T>()``: Gets the Value of the Variable with the specified Type.
- ``void Set<T>(T value)``: Sets the Value of the Variable with the specified Type.
# Examples
```cs
//Create a client which connects to the given server address
Client cli = new Client("opc.tcp://localhost:1337/");
//Get all objects of a specific type
ServerObject[] objects = cli.GetObjects(69);
//Check if a name exists
bool nameExists = cli.NameExists("myObject1");
//Check if a node id exists
bool nodeExists = cli.NodeIdExists("myNodeId1");
//Get an object by its name
ServerObject myObject1 = cli.GetObjectByName("myObject1");
//Call a method by its name in the object
IList<object> returnValue = myObject1.Method("CreateChildObject").Call("myObject2", 420u);
//Refresh the client's data to display the new object
cli.Refresh();
//Get an object by its node id
ServerObject myObject2 = cli.GetObjectByNodeId("myObject1-myObject2");
//Get a variable from an object by its name
VariableDescription id = myObject2.Variable("ObjectId");
//Get the value of the variable
uint oldValue = id.Get<uint>();
//Set the value of the variable
CellNumber.Set<uint>(69u);
//Get the updated value of the variable
uint newValue = id.Get<uint>();
```

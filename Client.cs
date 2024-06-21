using System.Collections.Generic; //Dictionary, IList, KeyValuePair
using Opc.Ua.Configuration; //ApplicationInstance
using Opc.Ua.Client; //Session, CoreClientUtils
#pragma warning disable IDE0011, IDE0056, IDE0090
namespace Opc.Ua.CustomClient
{
    /// <summary>
    /// Utility functions that can be used to get the children of an object or node id
    /// </summary>
    internal static class ClientExtensions
    {
        /// <summary>
        /// Gets the child objects of a specified parent object
        /// </summary>
        /// <param name="parent">Object to get the children from</param>
        /// <param name="sessionHandle">Client session</param>
        /// <param name="flags">Flags to specify which objects to get, all by default</param>
        /// <returns>Child objects with the specified flags</returns>
        public static ReferenceDescriptionCollection GetChildren(this ReferenceDescription parent, Session sessionHandle, uint flags = (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method)
        {
            //Get the object's expanded node id and call the underlying function
            return parent.NodeId.GetChildren(sessionHandle, flags);
        }

        /// <summary>
        /// Gets the child objects of an expanded node id
        /// </summary>
        /// <param name="exNodeId">Expanded node id to get the children from</param>
        /// <param name="sessionHandle">Client session</param>
        /// <param name="flags">Flags to specify which objects to get, all by default</param>
        /// <returns>Child objects with the specified flags</returns>
        public static ReferenceDescriptionCollection GetChildren(this ExpandedNodeId exNodeId, Session sessionHandle, uint flags = (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method)
        {
            //Convert the expanded node id to a node id and call the underlying function
            return ExpandedNodeId.ToNodeId(exNodeId, sessionHandle.NamespaceUris).GetChildren(sessionHandle, flags);
        }

        /// <summary>
        /// Gets the child objects of a node id
        /// </summary>
        /// <param name="nodeId">Node id to get the children from</param>
        /// <param name="sessionHandle">Client session</param>
        /// <param name="flags">Flags to specify which objects to get, all by default</param>
        /// <returns>Child objects with the specified flags</returns>
        public static ReferenceDescriptionCollection GetChildren(this NodeId nodeId, Session sessionHandle, uint flags = (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method)
        {
            //Browse the session objects with the specified flags and return all of them
            _ = sessionHandle.Browse(null,
                null,
                nodeId,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                flags,
                out _,
                out ReferenceDescriptionCollection refs);
            return refs;
        }
    }

    /// <summary>
    /// Object that describes a single opc ua method
    /// </summary>
    public class MethodDescription
    {
        private readonly Session SessionHandle;
        private readonly ReferenceDescription Handle;
        private readonly ServerObject Parent;
        public static implicit operator ReferenceDescription(MethodDescription md) => md.Handle;

        /// <summary>
        /// Constructor, sets the handle of the method object and the parent object
        /// </summary>
        /// <param name="desc">Handle of the method object</param>
        /// <param name="parent">Handle of the object that contains the method</param>
        public MethodDescription(ReferenceDescription desc, ServerObject parent, Session session)
        {
            Handle = desc;
            Parent = parent;
            SessionHandle = session;
        }

        /// <summary>
        /// Calls the method with the specified arguments, gets the return data as a list
        /// </summary>
        /// <param name="args">Optional parameters to call the function</param>
        /// <returns>List of return arguments</returns>
        public IList<object> Call(params object[] args)
        {
            //Nothing to do here if we're not connected to a server
            if (SessionHandle == null) return null;
            //Call the method by using the session, parent object and method object, providing the args
            return SessionHandle.Call(ExpandedNodeId.ToNodeId(Parent.GetHandle().NodeId, SessionHandle.NamespaceUris),
                ExpandedNodeId.ToNodeId(Handle.NodeId, SessionHandle.NamespaceUris),
                args);
        }
    }

    /// <summary>
    /// Object that describes a single opc ua variable
    /// </summary>
    public class VariableDescription
    {
        private readonly Session SessionHandle;
        private readonly ReferenceDescription Handle;
        public static implicit operator ReferenceDescription(VariableDescription vd) => vd.Handle;

        /// <summary>
        /// Constructor, sets the handle of the variable object
        /// </summary>
        /// <param name="desc">Handle of the variable object</param>
        public VariableDescription(ReferenceDescription desc, Session session)
        {
            Handle = desc;
            SessionHandle = session;
        }

        /// <summary>
        /// Sets the variable's value
        /// </summary>
        /// <typeparam name="T">Type of the variable. WARNING: Will do nothing if the type is wrong</typeparam>
        /// <param name="value">New value to set</param>
        public void Set<T>(T value)
        {
            //Create a write request with the variable's node id and new value
            WriteValue writeValue = new WriteValue
            {
                NodeId = ExpandedNodeId.ToNodeId(Handle.NodeId, SessionHandle.NamespaceUris),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };
            //Send the request to the server
            _ = SessionHandle.Write(null,
                new[] { writeValue },
                out _,
                out _);
        }

        /// <summary>
        /// Gets the variable's value
        /// </summary>
        /// <typeparam name="T">Type of the variable. WARNING: Will do nothing if the type is wrong</typeparam>
        /// <returns>Value of the variable</returns>
        public T Get<T>()
        {
            //Create a read request with the variable's node id
            ReadValueId rvi = new ReadValueId
            {
                NodeId = ExpandedNodeId.ToNodeId(Handle.NodeId, SessionHandle.NamespaceUris),
                AttributeId = Attributes.Value
            };
            //Send the request to the server, get the resulting data
            _ = SessionHandle.Read(null,
                0.0,
                TimestampsToReturn.Both,
                new[] { rvi },
                out DataValueCollection dvc,
                out _);
            //Return the first data entry, there should always be one, unless there are multiple variables with the same node id
            return (T)dvc[0].Value;
        }
    }

    /// <summary>
    /// Object that describes a single opc ua class
    /// </summary>
    public class ServerObject
    {
        internal readonly Session SessionHandle;
        internal readonly ReferenceDescription Handle;
        internal readonly ServerObject Parent;
        internal readonly Dictionary<string, MethodDescription> Methods;
        internal readonly Dictionary<string, VariableDescription> Variables;
        internal readonly Dictionary<string, ReferenceDescription> Children;
        internal int TypeId;
        internal string NodeId;

        /// <summary>
        /// Gets the actual handle of the object
        /// </summary>
        /// <returns>Object handle</returns>
        public ReferenceDescription GetHandle() => Handle;

        /// <summary>
        /// Gets the parent object that contains the current object
        /// </summary>
        /// <returns>Parent object if it exists</returns>
        public ServerObject GetParent() => Parent;

        /// <summary>
        /// Gets the node id of the object
        /// </summary>
        /// <returns>Object node id</returns>
        public string GetNodeId() => NodeId;

        /// <summary>
        /// Gets a method in the object by its name
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <returns>Method description on success, null on fail</returns>
        public MethodDescription Method(string name)
        {
            return string.IsNullOrEmpty(name) || !Methods.ContainsKey(name) ? null : Methods[name];
        }

        /// <summary>
        /// Gets a variable in the object by its name
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <returns>Variable description on success, null on fail</returns>
        public VariableDescription Variable(string name)
        {
            return string.IsNullOrEmpty(name) || !Variables.ContainsKey(name) ? null : Variables[name];
        }

        /// <summary>
        /// Gets the type id of the object
        /// </summary>
        /// <returns>Object type id</returns>
        public int GetTypeId() => TypeId;

        /// <summary>
        /// Internal constructor, sets the handle and the type of the object and populate its child objects
        /// </summary>
        /// <param name="desc">Object handle</param>
        /// <param name="parent">Parent object</param>
        /// <param name="nodeId">Node id of the object</param>
        internal ServerObject(ReferenceDescription desc, ServerObject parent, string nodeId, Session session)
        {
            Handle = desc;
            Parent = parent;
            NodeId = nodeId;
            SessionHandle = session;
            Methods = new Dictionary<string, MethodDescription>();
            Variables = new Dictionary<string, VariableDescription>();
            Children = new Dictionary<string, ReferenceDescription>();
            Populate();
            DefineTypeId();
        }

        /// <summary>
        /// Populates the type id if it exists
        /// </summary>
        private void DefineTypeId()
        {
            //Set the type id to a default value in case we can't find the actual one
            TypeId = -1;
            //If no handle exists, there's nothing more to do
            if (Handle == null) return;
            //Get the type definition node id
            NodeId typeNode = ExpandedNodeId.ToNodeId(Handle.TypeDefinition, SessionHandle.NamespaceUris);
            //If it doesn't exist, there's nothing more to do
            if (typeNode == null || typeNode.IdType != IdType.Numeric) return;
            //Convert the identifier to an int and set the type id
            _ = int.TryParse(typeNode.Identifier.ToString(), out TypeId);
        }

        /// <summary>
        /// Populates the child objects
        /// </summary>
        private void Populate()
        {
            //Get the current client session
            Session s = SessionHandle;
            //Get all method children of our object
            ReferenceDescriptionCollection children = Handle.GetChildren(s, (uint)NodeClass.Method);
            if (children != null)
            {
                //If there are any, loop through them
                foreach (ReferenceDescription method in children)
                {
                    //All objects that were created by us have string node ids, so we only care about them
                    if (method.NodeId.IdType != IdType.String) continue;
                    //Get the child's display name
                    string name = method.DisplayName.ToString();
                    //If the list already has this node, skip it
                    if (Methods.ContainsKey(name)) continue;
                    //Add the child to the list, using its name as a key
                    Methods.Add(name, new MethodDescription(method, this, s));
                }
            }
            //Get all variable children of our object
            children = Handle.GetChildren(s, (uint)NodeClass.Variable);
            if (children != null)
            {
                //If there are any, loop through them
                foreach (ReferenceDescription variable in children)
                {
                    //All objects that were created by us have string node ids, so we only care about them
                    if (variable.NodeId.IdType != IdType.String) continue;
                    //Get the child's display name
                    string name = variable.DisplayName.ToString();
                    //If the list already has this node, skip it
                    if (Variables.ContainsKey(name)) continue;
                    //Add the child to the list, using its name as a key
                    Variables.Add(name, new VariableDescription(variable, s));
                }
            }
            //Get all object children of our object
            children = Handle.GetChildren(s, (uint)NodeClass.Object);
            if (children != null)
            {
                //If there are any, loop through them
                foreach (ReferenceDescription child in children)
                {
                    //All objects that were created by us have string node ids, so we only care about them
                    if (child.NodeId.IdType != IdType.String) continue;
                    //Get the child's display name
                    string name = child.DisplayName.ToString();
                    //If the list already has this node, skip it
                    if (Children.ContainsKey(name)) continue;
                    //Add the child to the list, using its name as a key
                    Children.Add(name, child);
                }
            }
        }
    }

    /// <summary>
    /// Client for connecting to an opc ua server
    /// </summary>
    public class Client
    {
        private readonly string Endpoint;
        private Session SessionHandle;
        private ApplicationConfiguration Config;
        private EndpointDescription Desc;
        private Dictionary<string, ServerObject> Objects;

        /// <summary>
        /// Constructor, automatically connects to the opc ua server by using its endpoint
        /// </summary>
        /// <param name="serverEndpoint">Server endpoint or address to connect to</param>
        public Client(string serverEndpoint)
        {
            //Specify the endpoint and create a connection
            Endpoint = serverEndpoint;
            Refresh();
        }

        /// <summary>
        /// Refreshes the connection to the specified opc ua server
        /// </summary>
        public void Refresh()
        {
            //If we are already connected, close the connection
            if (SessionHandle != null)
                _ = SessionHandle.Close();
            //Reset all variables
            SessionHandle = null;
            Config = null;
            Desc = null;
            Objects = new Dictionary<string, ServerObject>();
            //Create a new connection to the endpoint
            CreateConnection(Endpoint, out Config, out Desc);
            //Create a session for the connection
            SessionHandle = Session.Create(Config, new ConfiguredEndpoint(null, Desc, EndpointConfiguration.Create(Config)), false, "", 60000, new UserIdentity(), null).GetAwaiter().GetResult();
            //Get all server objects
            PopulateServerObjects();
        }

        /// <summary>
        /// Refreshes the objects in the client
        /// </summary>
        public void RefreshObjects()
        {
            //Delete the old object dict by overwriting it
            Objects = new Dictionary<string, ServerObject>();
            //Populate the objects again to ensure that we know all of them
            PopulateServerObjects();
        }

        /// <summary>
        /// Refreshes the child objects of the specified object
        /// </summary>
        /// <param name="obj">Object to populate the childs from</param>
        public void RefreshChildObjects(ServerObject obj)
        {
            //Get the current client session
            Session s = SessionHandle;
            //If the object doesn't exist, there's nothing more to do
            if (obj == null) return;
            //Populate the child objects of the providied object
            PopulateServerObjects(obj, obj.Handle.GetChildren(s));
        }

        /// <summary>
        /// Gets the session for manually editing the server
        /// </summary>
        /// <returns>Client session</returns>
        public Session GetSession() => SessionHandle;

        /// <summary>
        /// Finds out if the client is connected to the server
        /// </summary>
        /// <returns>True if the client is connected, otherwise false</returns>
        public bool IsConnected()
        {
            Session ses = GetSession();
            return ses != null && ses.Connected;
        }

        /// <summary>
        /// Gets an object from the server by its name to edit it
        /// </summary>
        /// <param name="name">Name of the object</param>
        /// <returns>Handle of the specified object on success, null on fail</returns>
        public ServerObject GetObjectByName(string name)
        {
            //Find the node id of the name and call the underlying function
            return GetObjectByNodeId(NameToNodeId(name));
        }

        /// <summary>
        /// Gets an object from the server by its node id to edit it
        /// </summary>
        /// <param name="nodeId">Node id of the object</param>
        /// <returns>Handle of the specified object on success, null on fail</returns>
        public ServerObject GetObjectByNodeId(string nodeId)
        {
            //If the node id is null or it doesn't exist, there's nothing to do here
            if (string.IsNullOrEmpty(nodeId) || !Objects.ContainsKey(nodeId)) return null;
            //Get the object's handle by its node id
            return Objects[nodeId];
        }

        /// <summary>
        /// Finds out if an object with the specified name exists in the server
        /// </summary>
        /// <param name="name">Name of the object</param>
        /// <returns>True if the object exists</returns>
        public bool NameExists(string name)
        {
            //Find the node id of the name and call the underlying function
            return NodeIdExists(NameToNodeId(name));
        }

        /// <summary>
        /// Finds out if an object with the specified node id exists in the server
        /// </summary>
        /// <param name="nodeId">Node id of the object</param>
        /// <returns>True if the object exists</returns>
        public bool NodeIdExists(string nodeId)
        {
            //If the node id is null, there's nothing to do here
            if (string.IsNullOrEmpty(nodeId)) return false;
            //Check if the node id exists in the object list
            return Objects.ContainsKey(nodeId);
        }

        /// <summary>
        /// Finds out if an object with the specified name exists in the specified server object
        /// </summary>
        /// <param name="name">Name of the object to search</param>
        /// <param name="obj">Object that may contain the object name</param>
        /// <returns>True if the name exists in the server object</returns>
        public bool NameExistsInObject(string name, ServerObject obj)
        {
            //Find the node id of the name and call the underlying function
            return NodeIdExistsInObject(NameToNodeId(name), obj);
        }

        /// <summary>
        /// Finds out if an object with the specified node id exists in the specified server object
        /// </summary>
        /// <param name="nodeId">Node id of the object to search</param>
        /// <param name="obj">Object that may contain the object name</param>
        /// <returns>True if the node id exists in the server object</returns>
        public bool NodeIdExistsInObject(string nodeId, ServerObject obj)
        {
            //If the node id doesn't exist, there's nothing to do here
            if (!NodeIdExists(nodeId)) return false;
            //All objects that were created by us have string node ids, so we only care about them
            if (obj.GetHandle().NodeId.IdType != IdType.String) return false;
            //Get the node id value and check if it exists in the server
            string key = obj.GetHandle().NodeId.Identifier.ToString();
            if (!Objects.ContainsKey(key)) return false;
            //Split the node id to find the object name
            string[] nodeIdParts = nodeId.Split('.');
            //If the server object has a child with the name, it exists
            return Objects[key].Children.ContainsKey(nodeIdParts[nodeIdParts.Length - 1]);
        }

        /// <summary>
        /// Gets all objects of the server
        /// </summary>
        /// <returns>All objects</returns>
        public ServerObject[] GetObjects()
        {
            //Call the underlying function, -1 means that no filter is provided
            return GetObjects(-1);
        }

        /// <summary>
        /// Gets all objects of the server that have the specified type id
        /// </summary>
        /// <param name="typeIdFilter">Type id that the objects should have</param>
        /// <returns>All objects with the type id</returns>
        public ServerObject[] GetObjects(int typeIdFilter)
        {
            //Create a result buffer as big as the entire object array
            ServerObject[] result = new ServerObject[Objects.Count];
            //Counter that sets the index in the result buffer
            int counter = 0;
            //Loop through the object list, compare the object's type and add it to the result buffer if it matches
            foreach (KeyValuePair<string, ServerObject> obj in Objects)
                if (typeIdFilter == -1 || obj.Value.GetTypeId() == typeIdFilter)
                    result[counter++] = obj.Value;
            //Create a new result buffer with the actual length of the result
            ServerObject[] actualResult = new ServerObject[counter];
            //Reset the counter to the start again
            counter = 0;
            //Loop through our result
            foreach (ServerObject obj in result)
            {
                //If an object is null, it's guaranteed that no objects come after it, so we're done
                if (obj == null) break;
                //Copy the result to our new result buffer
                actualResult[counter++] = obj;
            }
            return actualResult;
        }

        /// <summary>
        /// Converts an object's name to its node id
        /// </summary>
        /// <param name="name">Name of the object to search</param>
        /// <returns>Node id of the object on success, null on fail</returns>
        private string NameToNodeId(string name)
        {
            //Loop through the object list, if we find one with a matching name, we can return its key
            foreach (KeyValuePair<string, ServerObject> obj in Objects)
                if (obj.Value.GetHandle().DisplayName.ToString() == name || obj.Value.GetHandle().BrowseName.ToString() == name)
                    return obj.Key;
            //Object not found
            return null;
        }

        /// <summary>
        /// Creates and prepares a connection to a specific endpoint, needed to create a session
        /// </summary>
        /// <param name="endpoint">Server endpoint</param>
        /// <param name="cfg">Resulting app config</param>
        /// <param name="desc">Resulting endpoint description</param>
        private static void CreateConnection(string endpoint, out ApplicationConfiguration cfg, out EndpointDescription desc)
        {
            //Create a default app config
            cfg = new ApplicationConfiguration
            {
                ApplicationName = "OPC-UA Client Lib",
                ApplicationUri = "urn:localhost:UA:OUAC",
                ProductUri = "http://opcfoundation.org/UA/OUAC",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration()
                {
                    ApplicationCertificate = new CertificateIdentifier()
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\pki\own",
                        SubjectName = "CN=OUAC, C=US, S=Arizona, O=OPC Foundation, DC=localhost"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList()
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\pki\issuer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList()
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\pki\trusted"
                    },
                    RejectedCertificateStore = new CertificateStoreIdentifier()
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\pki\rejected"
                    }
                },
                TransportQuotas = new TransportQuotas()
                {
                    OperationTimeout = 600000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                },
                ClientConfiguration = new ClientConfiguration() { DefaultSessionTimeout = 60000 }
            };
            //Replace the certificate validation with a dummy function that accepts any certificate
            cfg.CertificateValidator.CertificateValidation += (s, e) => e.Accept = true;
            //Create a new app instance for the client by using the config
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = cfg
            };
            //Check the certificate, required to connect to anything
            _ = application.CheckApplicationInstanceCertificate(false, 0).GetAwaiter().GetResult();
            //Set the output description
            desc = CoreClientUtils.SelectEndpoint(cfg, endpoint, true, 15000);
        }

        /// <summary>
        /// Populates the child objects of the provided object list
        /// </summary>
        /// <param name="parent">Parent object that contains the list</param>
        /// <param name="children">List of objects to populate</param>
        private void PopulateServerObjects(ServerObject parent, ReferenceDescriptionCollection children)
        {
            //Get the current client session
            Session s = SessionHandle;
            //If there are no children, there's nothing more to do here
            if (children == null) return;
            //Otherwise, loop through all of them
            foreach (ReferenceDescription desc in children)
            {
                //Push the object to our object list
                PushObject(desc, parent);
                //Get the child objects
                ReferenceDescriptionCollection childs = desc.GetChildren(s);
                //Convert our object to a serverobject
                ServerObject so = ConvertToServerObject(desc);
                //Call the function again for the new objects
                PopulateServerObjects(so, childs);
            }
        }

        /// <summary>
        /// Populates the child objects of the server
        /// </summary>
        private void PopulateServerObjects()
        {
            //Get the current client session
            Session s = SessionHandle;
            //Get the child objects of the server, this should contain the line object
            ReferenceDescriptionCollection serverChildren = ObjectIds.ObjectsFolder.GetChildren(s);
            //Populate all child objects of the line
            PopulateServerObjects(null, serverChildren);
        }

        /// <summary>
        /// Converts an object handle to a server object if the server object exists
        /// </summary>
        /// <param name="desc">Object handle to convert</param>
        /// <returns>Server object</returns>
        private ServerObject ConvertToServerObject(ReferenceDescription desc)
        {
            //Nothing to do here if the handle is null
            if (desc == null) return null;
            //All objects that were created by us have string node ids, so we only care about them
            if (desc.NodeId.IdType != IdType.String) return null;
            //Get the object's node id as a string
            string key = desc.NodeId.Identifier.ToString();
            //If the node id doesn't exist, there's nothing more to do
            if (!Objects.ContainsKey(key)) return null;
            //Return the server object from the object list
            return Objects[key];
        }

        /// <summary>
        /// Adds an object to the object list by its handle
        /// </summary>
        /// <param name="obj">Object handle to add</param>
        /// <param name="parent">Parent object</param>
        private void PushObject(ReferenceDescription obj, ServerObject parent)
        {
            //All objects that were created by us have string node ids, so we only care about them
            if (obj.NodeId.IdType != IdType.String) return;
            //Get the object's node id as a string
            string key = obj.NodeId.Identifier.ToString();
            //If the list already has this node, skip it
            if (Objects.ContainsKey(key)) return;
            //Otherwise, add it to the list with its node id as a key
            Objects.Add(key, new ServerObject(obj, parent, key, SessionHandle));
        }
    }
}
#pragma warning restore IDE0011, IDE0056, IDE0090
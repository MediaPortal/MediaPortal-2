using System;
using System.Collections.Generic;
using HttpServer.Rendering.Haml.Nodes;

namespace HttpServer.Rendering.Haml
{
    /// <summary>
    /// A list of prototype nodes.
    /// </summary>
    public class NodeList
    {
        private readonly List<Node> _nodes = new List<Node>();

        /// <summary>
        /// Creates the node.
        /// </summary>
        /// <param name="word">node identifier.</param>
        /// <param name="parent">parent node.</param>
        /// <returns>created node if identifier was found; otherwise null.</returns>
        public Node CreateNode(string word, Node parent)
        {
            foreach (Node node in _nodes)
            {
                if (node.CanHandle(word, parent == null))
                    return (Node) Activator.CreateInstance(node.GetType(), new object[]{parent});
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="word"></param>
        /// <param name="firstNode">first node on line</param>
        /// <returns></returns>
        public Node GetPrototype(string word, bool firstNode)
        {
            foreach (Node node in _nodes)
            {
                if (node.CanHandle(word, firstNode))
                    return node;
            }

            return null;            
        }

        /// <summary>
        /// Add a prototype
        /// </summary>
        /// <param name="node">prototype node</param>
        public void Add(Node node)
        {
            //todo: Replace types.
            _nodes.Add(node);
        }
    }
}

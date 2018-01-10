﻿#region namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;
using System.Globalization;
using Graphical.Base;
#endregion

namespace Graphical.Graphs
{
    
    /// <summary>
    /// Representation of a Graph.
    /// Graph contains a Dictionary where
    /// </summary>
    public class Graph : IGraphicItem, ICloneable
    {
        #region Variables
        internal Guid graphID { get; private set; }
        internal Dictionary<int, gPolygon> polygons = new Dictionary<int, gPolygon>();
        internal int pId = 0;
        internal Dictionary<gVertex, List<gEdge>> graph = new Dictionary<gVertex, List<gEdge>>();
        /// <summary>
        /// Graph's vertices
        /// </summary>
        public List<gVertex> vertices { get { return graph.Keys.ToList(); } }

        /// <summary>
        /// Graph's edges
        /// </summary>
        public List<gEdge> edges { get; set; }

        #endregion

        #region Constructors
        internal Graph()
        {
            edges = new List<gEdge>();
            graphID = Guid.NewGuid();
        }

        internal Graph(List<gPolygon> input)
        {
            edges = new List<gEdge>();
            graphID = Guid.NewGuid();
            //Setting up Graph instance by adding vertices, edges and polygons
            for (var i = 0; i < input.Count(); i++)
            {
                gPolygon gPolygon = input[i];
                List<gVertex> vertices = gPolygon.vertices;
                gPolygon.edges.Clear();
                //If there is only one polygon, treat is as boundary
                if(input.Count() == 1)
                {
                    gPolygon.isBoundary = true;
                }

                //If first and last point of vertices list are the same, remove last.
                if (vertices.First().Equals(vertices.Last()) && vertices.Count() > 1)
                {
                    vertices = vertices.Take(vertices.Count() - 1).ToList();
                }
                int vertexCount = vertices.Count();

                //For each point, creates vertex and associated edge and adds them
                //to the polygons Dictionary
                for (var j = 0; j < vertexCount; j++)
                {
                    int next_index = (j + 1) % vertexCount;
                    gVertex vertex = vertices[j];
                    gVertex next_vertex = vertices[next_index];
                    gEdge edge = new gEdge(vertex, next_vertex);
                    //If is a valid vertices, add id to vertex and
                    //edge to vertices dictionary
                    if (vertexCount > 2)
                    {
                        vertex.polygonId = pId;
                        next_vertex.polygonId = pId;
                        gPolygon gPol = new gPolygon();
                        if (polygons.TryGetValue(pId, out gPol))
                        {
                            gPol.edges.Add(edge);
                        }
                        else
                        {
                            gPolygon.edges.Add(edge);
                            gPolygon.id = pId;
                            polygons.Add(pId, gPolygon);
                            
                        }
                    }
                    AddEdge(edge);
                }

                if (vertexCount > 2) {
                    pId += 1;
                }else
                {
                    gPolygon.Dispose();
                }
            }
        }

        
        internal static List<gPolygon> FromPolygons(Polygon[] polygons, bool isExternal)
        {
            if(polygons == null) { throw new NullReferenceException("polygons"); }
            List<gPolygon> input = new List<gPolygon>();
            foreach(Polygon pol in polygons)
            {
                gPolygon gPol = new gPolygon(-1, isExternal);
                gPol.vertices = pol.Points.Select(pt => gVertex.ByPoint(pt)).ToList();
                input.Add(gPol);
            }

            return input;
        }

        internal void AddVertices (List<gVertex> vertices)
        {

        }
        #endregion

        #region Methods

        /// <summary>
        /// Contains mathod for vertex in graph
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        internal bool Contains(gVertex vertex)
        {
            return graph.ContainsKey(vertex);
        }

        /// <summary>
        /// Contains method for edges in graph
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        internal bool Contains(gEdge edge)
        {
            return edges.Contains(edge);
        }

        internal List<gEdge> GetVertexEdges(gVertex vertex)
        {
            List<gEdge> edgesList = new List<gEdge>();
            if(graph.TryGetValue(vertex, out edgesList))
            {
                return edgesList;
            }else
            {
                graph.Add(vertex, new List<gEdge>());
                return graph[vertex];
            }
        }

        internal List<gVertex> GetAdjecentVertices(gVertex v)
        {
            return graph[v].Select(edge => edge.GetVertexPair(v)).ToList();
        }
        /// <summary>
        /// Add edge to the analisys graph
        /// </summary>
        /// <param name="edge">New edge</param>
        internal void AddEdge(gEdge edge)
        {
            List<gEdge> startEdgesList = new List<gEdge>();
            List<gEdge> endEdgesList = new List<gEdge>();
            if (graph.TryGetValue(edge.StartVertex, out startEdgesList))
            {
                startEdgesList.Add(edge);
            }
            else
            {
                graph.Add(edge.StartVertex, new List<gEdge>() { edge });
            }

            if (graph.TryGetValue(edge.EndVertex, out endEdgesList))
            {
                endEdgesList.Add(edge);
            }
            else
            {
                graph.Add(edge.EndVertex, new List<gEdge>() { edge });
            }
            
            if (!edges.Contains(edge)) { edges.Add(edge); }
        }

        /// <summary>
        /// Get vertices boundaries on graph
        /// </summary>
        /// <returns name="polygons[]"></returns>
        public List<gEdge>[] GetBoundaryPolygons()
        {
            return polygons.Values.ToList().Select(p => p.edges).ToArray();
        }


        #endregion

        #region Override Methods
        //TODO: Improve overriding equality methods as per http://www.loganfranken.com/blog/687/overriding-equals-in-c-part-1/


        /// <summary>
        /// Override of ToStringMethod
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("VisibilityGraph:(gVertices: {0}, gEdges: {1})", vertices.Count.ToString(), edges.Count.ToString());
        }

        /// <summary>
        /// Customizing the render of gVertex
        /// </summary>
        /// <param name="package"></param>
        /// <param name="parameters"></param>
        [IsVisibleInDynamoLibrary(false)]
        public void Tessellate(IRenderPackage package, TessellationParameters parameters)
        {
            foreach(gVertex v in vertices)
            {
                v.Tessellate(package, parameters);
            }
            foreach(gEdge e in edges)
            {
                e.Tessellate(package, parameters);
            }
        }

        public virtual object Clone()
        {
            Graph newGraph = new Graph()
            {
                graph = new Dictionary<gVertex, List<gEdge>>(this.graph),
                edges = new List<gEdge>(this.edges),
                polygons = new Dictionary<int, gPolygon>(this.polygons)
            };
            return newGraph;
        }
        #endregion

    }
}
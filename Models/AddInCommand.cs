using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Entools.Model.LibTools;

namespace Entools.Model
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]

    public class Entools : IExternalCommand
    {
        #region IExternalCommand Members Implementation
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            try
            {
                PathFinder pathFinder = new PathFinder();
                UIApplication uiapp = revit.Application;
                pathFinder.Main(uiapp);

                return Autodesk.Revit.UI.Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Autodesk.Revit.UI.Result.Failed;
            }
        }
        #endregion IExternalCommand Members Implementation
    }

    public class PathFinder
    {
        public void Main(UIApplication uiapp)
        {
            Transfer.doc = uiapp.ActiveUIDocument.Document;
            LibCategories libCategories = new LibCategories();
            Selection sel = uiapp.ActiveUIDocument.Selection;
            ElementId eId1 = null, eId = null;
            ISelectionFilter selFilt = new SystemElSelectionFilter();

            List<BuiltInCategory> incCat = new List<BuiltInCategory>()
            {
                BuiltInCategory.OST_CableTrayFitting,
                BuiltInCategory.OST_CableTray
            };

            List<Element> lst = libCategories.AllElnewe(Transfer.doc, incCat).ToList();

            try
            {
                while (eId == null || eId1 == null)
                {
                    eId1 = sel.PickObject(ObjectType.Element, selFilt, "Select start point of system").ElementId;
                    eId = sel.PickObject(ObjectType.Element, selFilt, "Select end point of system").ElementId;
                }
            }
            catch
            {
                MessageBox.Show("Canceled");
            }

            int size = lst.Count();
            List<Vertex> graph = FindVertex(size, lst);

            Finder(graph, lst, eId, eId1);
        }

        #region ALG
        private static int MinDistance(double[] distance, bool[] shortestPathTreeSet, int verticesCount)
        {
            double min = int.MaxValue; int minIndex = 0;

            for (int v = 0; v < verticesCount; ++v)
            {
                if (!shortestPathTreeSet[v] && distance[v] <= min)
                {
                    min = distance[v];
                    minIndex = v;
                }
            }

            return minIndex;
        }

        public static void DijkstraAlgo(List<Vertex> graph, int source, int verticesCount,
            IList<Element> lstEls, ElementId eId1, ElementId eId)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            List<int> pathList = new List<int>();

            double[] distance = new double[verticesCount];
            bool[] shortestPathTreeSet = new bool[verticesCount];
            int startPos = 0, finishPos = 0;

            for (int i = 0; i < verticesCount; ++i)
            {
                distance[i] = int.MaxValue;
                shortestPathTreeSet[i] = false;
                //dict.Add(i,0);

                if (lstEls[i].Id == eId) startPos = i;
                if (lstEls[i].Id == eId1) finishPos = i;
            }

            distance[source] = 0;

            for (int count = 0; count < verticesCount - 1; ++count)
            {
                int u = MinDistance(distance, shortestPathTreeSet, verticesCount);
                shortestPathTreeSet[u] = true;

                for (int v = 0; v < verticesCount; ++v)
                    if (!shortestPathTreeSet[v] &&
                        Convert.ToBoolean(graph[u].Connections[v]) &&
                        distance[u] != int.MaxValue &&
                        distance[u] + graph[u].Connections[v] < distance[v])
                    {
                        distance[v] = distance[u] + graph[u].Connections[v];
                        dict[v] = u;
                    }
            }

            pathList.Add(finishPos);
            int index = dict[finishPos];

            while (index != startPos)
            {
                pathList.Add(index);
                index = dict[index];
            }

            pathList.Add(startPos);

            SetNumber(distance, verticesCount, pathList, startPos, finishPos, lstEls);
        }
        #endregion

        public void Finder(List<Vertex> graph, IList<Element> lst, ElementId eId, ElementId eId1)
        {
            int size = lst.Count(), startPointIndex = 0;

            foreach (Vertex vertex in graph) // Build graph
            {
                if (vertex.ElementId == eId) break;
                startPointIndex++;
            }

            DijkstraAlgo(graph, startPointIndex, size, lst, eId1, eId);
        }

        private static void SetNumber(double[] distance, int verticesCount, List<int> pathList,
            int startPos, int finishPos, IList<Element> lst)
        {
            List<ElementId> listIds = new List<ElementId>();

            foreach (var sec in pathList)
            {
                listIds.Add(lst[sec].Id);
            }

            using (Transaction tx = new Transaction(Transfer.doc))
            {
                tx.Start("Set value");

                Element element;

                foreach (ElementId eId in listIds)
                {
                    element = Transfer.doc.GetElement(eId);

                    if (element.LookupParameter("Комментарий") != null)
                        element.LookupParameter("Комментарий").Set("Кабель-1"); 
                    // Записываем комментарий -1- в параметр 
                }

                tx.Commit();
            }
        }

        public List<Vertex> FindVertex(int size, List<Element> lstEls)
        {
            List<Vertex> graph = new List<Vertex>();

            for (int i = 0; i < size; i++)
            {
                Vertex vertex = new Vertex
                {
                    ElementId = lstEls[i].Id
                };

                List<Element> delta = LibConnectors.ElLstWith(lstEls[i]);

                for (int k = 0; k < size; k++)
                {
                    foreach (Element el in delta)
                        if (el.Id != lstEls[i].Id)
                            if (el.Id == lstEls[k].Id)
                            {
                                BuiltInParameter bipLeng = BuiltInParameter.CURVE_ELEM_LENGTH;

                                if (el.get_Parameter(bipLeng) != null) //для лотков
                                    vertex.Connections[k] = el.get_Parameter(bipLeng).AsDouble() / 304.8;
                                else vertex.Connections[k] = .05; // значение для фиттингов
                            }
                }

                graph.Add(vertex);
            }

            return graph;
        }
    }

    #region DATA
    public static class Transfer
    {
        public static Document doc = null;
    }

    public class Vertex
    {
        public ElementId ElementId;
        public double[] Connections = new double[5000];
    }

    class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }
    #endregion

    #region FILTER
    public class SystemElSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            Category category = element.Category;
            BuiltInCategory bic = (BuiltInCategory)category.Id.IntegerValue;

            if (bic == BuiltInCategory.OST_CableTrayFitting ||
                bic == BuiltInCategory.OST_CableTray)
                return true;

            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }
    #endregion
}
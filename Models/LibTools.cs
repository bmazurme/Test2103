using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Entools.Model.LibTools
{
    #region GETELEMENTS
    class LibCategories
    {
        public IList<Element> AllElnewe(Document doc, List<BuiltInCategory> listBuiltInCategories)
        {
            List<Element> listElements = new List<Element>();

            foreach (BuiltInCategory category in listBuiltInCategories)
            {
                ElementCategoryFilter elementCategoryFilter = new ElementCategoryFilter(category);
                FilteredElementCollector filteredElementCollector = new FilteredElementCollector(doc);
                IList<Element> listDelta = filteredElementCollector.WherePasses(elementCategoryFilter)
                    .WhereElementIsNotElementType().ToElements();
                listElements.AddRange(listDelta);
            }

            return listElements;
        }
    }
    #endregion

    #region GETCONNECTORS
    class LibConnectors
    {
        private static ConnectorManager GetConnectorManager(Element element)
        {

            MEPCurve сurve = element as MEPCurve;
            FamilyInstance fittings = element as FamilyInstance;

            if (null == сurve && null == fittings) throw new ArgumentException("Выбранный элемент не является допустимым");

            return null == сurve ? fittings.MEPModel.ConnectorManager : сurve.ConnectorManager;
        }

        public static List<Element> ElLstWith(Element element)
        {
            List<Element> listId = new List<Element>() { element };
            ConnectorManager getConnectorManager = GetConnectorManager(element);
            ConnectorSet connectors = getConnectorManager.Connectors;

            foreach (Connector con1 in connectors)
            {
                if (con1.IsConnected)
                {
                    ConnectorSet con2 = con1.ConnectorManager.Connectors;

                    foreach (Connector con3 in con2)
                        if (con3.IsConnected)
                        {
                            ConnectorSet con4 = con3.ConnectorManager.Connectors;

                            foreach (Connector con5 in con4)
                            {
                                ConnectorSet alRef = con5.AllRefs;

                                foreach (Connector c6 in alRef)
                                {
                                    if (c6.ConnectorType != ConnectorType.End
                                        || c6.Owner.Id.IntegerValue.Equals(con5.Owner.Id.IntegerValue))
                                        continue;

                                    listId.Add(c6.Owner);
                                }
                            }
                        }
                }
            }

            return listId.Where(e=>e!=null).GroupBy(e => e.Id).Select(e => e.First()).ToList();
        }
    }
    #endregion


}
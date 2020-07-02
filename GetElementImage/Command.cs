#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace GetElementImage
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Allow only family instances to be selected.
    /// </summary>
    class FamilyInstanceSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is FamilyInstance;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }


    /// <summary>
    /// Return a single preselected element
    /// or prompt user to select one.
    /// </summary>
    static Result GetSingleSelectedElement(
      UIDocument uidoc,
      ref string message,
      out Element e )
    {
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;
      ICollection<ElementId> ids = sel.GetElementIds();
      int n = ids.Count;

      e = null;

      if( 1 == n )
      {
        foreach( ElementId id in ids )
        {
          e = doc.GetElement( id );
        }
      }
      else if( 0 == n )
      {
        try
        {
          Reference r = sel.PickObject(
            ObjectType.Element, 
            new FamilyInstanceSelectionFilter(),
            "Please select element to export its views" );

          e = doc.GetElement( r.ElementId );
        }
        catch( OperationCanceledException )
        {
          return Result.Cancelled;
        }
      }
      else
      {
        message = "Please launch this command with "
          + "at most one pre-selected element";

        return Result.Failed;
      }
      return Result.Succeeded;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;
      Element e;

      Result rc = GetSingleSelectedElement( 
        uidoc, ref message, out e );

      if( Result.Succeeded == rc )
      {
      }

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .OfCategory( BuiltInCategory.INVALID )
          .OfClass( typeof( Wall ) );


      return Result.Succeeded;
    }
  }
}

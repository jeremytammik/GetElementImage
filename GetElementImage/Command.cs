#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace GetElementImage
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    #region Element Selection
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
    /// Return the preselected elements
    /// or prompt user to select some.
    /// </summary>
    static Result GetSelectedElements(
      UIDocument uidoc,
      ref string message,
      out ICollection<ElementId> ids )
    {
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;
      ids = sel.GetElementIds();
      int n = ids.Count;

      if( 0 == n )
      {
        try
        {
          IList<Reference> refs = sel.PickObjects(
            ObjectType.Element, 
            new FamilyInstanceSelectionFilter(),
            "Please select elements to export their views" );

          ids = new List<ElementId>( 
            refs.Select( r => r.ElementId ) );
        }
        catch( OperationCanceledException )
        {
          return Result.Cancelled;
        }
      }
      else
      {
        // check that all pre-selected elements match our criteria

        //message = "Invalid pre-selected elements: ...";
        //return Result.Failed;
      }
      return Result.Succeeded;
    }
    #endregion // Element Selection

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;
      ICollection<ElementId> ids;

      Result rc = GetSelectedElements( 
        uidoc, ref message, out ids );

      if( Result.Succeeded == rc )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Export PNG Element Images" );

          // Clear selection to unhighlight elements

          uidoc.Selection.SetElementIds(
            new List<ElementId>() );

          ImageExporter ie = new ImageExporter( doc );

          foreach( ElementId id in ids )
          {
            Element e = doc.GetElement( id );
            string filename = ie.ExportToImage( e );
            Debug.Print( "{0}: {1}", e.Id, filename );
          }
          tx.RollBack();
        }
      }
      return rc;
    }
  }
}

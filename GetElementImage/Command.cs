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
    /// Return a single preselected element
    /// or prompt user to select one.
    /// </summary>
    public static Result GetSingleSelectedElement(
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
            ObjectType.Element, "Please select element "
              + "to view in external browser" );

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

    #region Element pre- or post-selection
    static public ICollection<ElementId> GetSelectedElements(
      UIDocument uidoc )
    {
      // Do we have any pre-selected elements?

      Selection sel = uidoc.Selection;

      ICollection<ElementId> ids = sel.GetElementIds();

      // If no elements were pre-selected, 
      // prompt for post-selection

      if( null == ids || 0 == ids.Count )
      {
        IList<Reference> refs = null;

        try
        {
          refs = sel.PickObjects( ObjectType.Element,
            "Please select elements for 2D outline generation." );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return ids;
        }
        ids = new List<ElementId>(
          refs.Select<Reference, ElementId>(
            r => r.ElementId ) );
      }
      return ids;
    }

    /// <summary>
    /// Allow only room to be selected.
    /// </summary>
    class RoomSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is Room;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }

    static public IEnumerable<ElementId> GetSelectedRooms(
      UIDocument uidoc )
    {
      Document doc = uidoc.Document;

      // Do we have any pre-selected elements?

      Selection sel = uidoc.Selection;

      IEnumerable<ElementId> ids = sel.GetElementIds()
        .Where<ElementId>( id
          => (doc.GetElement( id ) is Room) );

      // If no elements were pre-selected, 
      // prompt for post-selection

      if( null == ids || 0 == ids.Count() )
      {
        IList<Reference> refs = null;

        try
        {
          refs = sel.PickObjects( ObjectType.Element,
            new RoomSelectionFilter(),
            "Please select rooms for 2D outline generation." );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return ids;
        }
        ids = new List<ElementId>(
          refs.Select<Reference, ElementId>(
            r => r.ElementId ) );
      }
      return ids;
    }
    #endregion // Element pre- or post-selection

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Access current selection

      Selection sel = uidoc.Selection;

      // Retrieve elements from database

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .OfCategory( BuiltInCategory.INVALID )
          .OfClass( typeof( Wall ) );

      // Filtered element collector is iterable

      foreach( Element e in col )
      {
        Debug.Print( e.Name );
      }

      // Modify document within a transaction

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Transaction Name" );
        tx.Commit();
      }

      return Result.Succeeded;
    }
  }
}

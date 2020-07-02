#region Namespaces
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace GetElementImage
{
  class ImageExporter
  {
    List<View> _views_to_export 
      = new List<View>( 1 );

    public ImageExporter( Document doc )
    {
      ViewFamilyType viewFamilyType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( ViewFamilyType ) )
          .Cast<ViewFamilyType>()
          .FirstOrDefault( x =>
            x.ViewFamily == ViewFamily.ThreeDimensional );

      Debug.Assert( null != viewFamilyType );

      View3D view3d = View3D.CreateIsometric( 
        doc, viewFamilyType.Id );

      Debug.Assert( null != view3d );

      _views_to_export.Add( view3d );

      Parameter graphicDisplayOptions
        = view3d.get_Parameter(
          BuiltInParameter.MODEL_GRAPHICS_STYLE );

      // Settings for best quality

      graphicDisplayOptions.Set( 6 );
    }

    public string ExportToImage( Element e )
    {
      Document doc = e.Document;

      // Hide all other elements in view

      View view = _views_to_export[ 0 ];
      FilteredElementCollector visible_elements = new FilteredElementCollector( doc, view.Id );
      ICollection<ElementId> ids = visible_elements.ToElementIds();
      view.HideElements( ids );
      ids.Clear();
      ids.Add( e.Id );
      view.UnhideElements( ids );

      var tempFileName = Path.ChangeExtension(
        Path.GetRandomFileName(), "png" );

      string tempImageFile;

      try
      {
        tempImageFile = Path.Combine(
          Path.GetTempPath(), tempFileName );
      }
      catch( IOException )
      {
        return null;
      }

      var ieo = new ImageExportOptions
      {
        FilePath = tempImageFile,
        FitDirection = FitDirectionType.Horizontal,
        HLRandWFViewsFileType = ImageFileType.PNG,
        ImageResolution = ImageResolution.DPI_150,
        ShouldCreateWebSite = false
      };

      int n = _views_to_export.Count;

      if( 0 < n )
      {
        List<ElementId> ids2 = new List<ElementId>( 
          _views_to_export.Select<View, ElementId>( 
            v => v.Id ) );

        ieo.SetViewsAndSheets( ids2 );
        ieo.ExportRange = ExportRange.SetOfViews;
      }
      else
      {
        ieo.ExportRange = ExportRange
          .VisibleRegionOfCurrentView;
      }

      ieo.ZoomType = ZoomFitType.FitToPage;
      ieo.ViewName = "tmp";

      if( ImageExportOptions.IsValidFileName(
        tempImageFile ) )
      {
        // If ExportRange = ExportRange.SetOfViews 
        // and document is not active, then image 
        // exports successfully, but throws
        // Autodesk.Revit.Exceptions.InternalException

        try
        {
          doc.ExportImage( ieo );
        }
        catch
        {
          return string.Empty;
        }
      }
      else
      {
        return string.Empty;
      }

      // File name has format like 
      // "tempFileName - view type - view name", e.g.
      // "luccwjkz - 3D View - {3D}.png".
      // Get the first image (we only listed one view
      // in views).

      var files = Directory.GetFiles(
        Path.GetTempPath(),
        string.Format( "{0}*.*", Path
          .GetFileNameWithoutExtension(
            tempFileName ) ) );

      return files.Length > 0
        ? files[ 0 ]
        : string.Empty;
    }
  }
}

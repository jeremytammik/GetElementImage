#region Namespaces
using System;
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
    /// <summary>
    /// View directions to export
    /// </summary>


    List<View> _views_to_export;
    //List<ElementId> _category_ids_to_hide;

    /// <summary>
    /// Categories to hide
    /// </summary>
    BuiltInCategory[] _categories_to_hide
      = new BuiltInCategory[]
    {
      BuiltInCategory.OST_Cameras,
      BuiltInCategory.OST_Levels,
      BuiltInCategory.OST_ProjectBasePoint
    };

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

      view3d.Name = "Isometric";

      Debug.Assert( null != view3d );

      _views_to_export = new List<View>( 1 ) { view3d };

      Parameter graphicDisplayOptions
        = view3d.get_Parameter(
          BuiltInParameter.MODEL_GRAPHICS_STYLE );

      // Settings for best quality

      graphicDisplayOptions.Set( 6 );

      // Get categories to hide

      //_category_ids_to_hide = new List<ElementId>( 2 );

      Categories cats = doc.Settings.Categories;

      //_category_ids_to_hide.Add( cats.get_Item( // null object
      //  BuiltInCategory.OST_Cameras ).Id );
      //_category_ids_to_hide.Add( cats.get_Item( 
      //  BuiltInCategory.OST_Levels ).Id );
      //_category_ids_to_hide.Add( cats.get_Item( 
      //  BuiltInCategory.OST_ProjectBasePoint ).Id );

      foreach( BuiltInCategory bic in _categories_to_hide )
      {
        Category cat = cats.get_Item( bic );

        if( null == cat )
        {
          Debug.Print( "{0} returns null category.", bic );
        }
        else
        {
          view3d.SetCategoryHidden( cat.Id, true );
        }

        // BuiltInCategory.OST_Cameras throws exception:
        // Autodesk.Revit.Exceptions.ArgumentException
        // Category cannot be hidden.
        // Parameter name: categoryId
        //ElementId id = new ElementId( bic );
        //view3d.SetCategoryHidden( id, true );
      }
    }

    public string ExportToImage( Element e )
    {
      Document doc = e.Document;

      // Hide all other elements in view

      View view = _views_to_export[ 0 ];

      List<ElementId> hideable_element_ids
        = new FilteredElementCollector( doc, view.Id )
          .Where<Element>( a => a.CanBeHidden( view ) )
          .Select<Element, ElementId>( b => b.Id )
          .ToList<ElementId>();

      view.HideElements( hideable_element_ids );

      //view.HideCategoriesTemporary( _category_ids_to_hide );

      List<ElementId> ids = new List<ElementId>( 1 ) { e.Id };

      view.UnhideElements( ids );

      doc.Regenerate();

      //string directory = Path.GetTempPath();

      //string tempFileName = Path.ChangeExtension(
      //  Path.GetRandomFileName(), "png" );

      //string tempImageFile;

      //try
      //{
      //  tempImageFile = Path.Combine(
      //    directory, tempFileName );
      //}
      //catch( IOException )
      //{
      //  return null;
      //}


      string dir = "C:/tmp";
      string fn = e.Id.IntegerValue.ToString();
      string filepath = $"{dir}/{fn}.png";

      var ieo = new ImageExportOptions
      {
        FilePath = filepath,
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

      //if( ImageExportOptions.IsValidFileName(
      //  tempImageFile ) )
      {
        // If ExportRange = ExportRange.SetOfViews 
        // and document is not active, then image 
        // exports successfully, but throws
        // Autodesk.Revit.Exceptions.InternalException

        try
        {
          doc.ExportImage( ieo );
        }
        catch(Exception ex)
        {
          //return string.Empty;
          Debug.Print( ex.Message );
        }
      }
      //else
      //{
      //  return string.Empty;
      //}

      // File name has format like 
      // "tempFileName - view type - view name", e.g.
      // "luccwjkz - 3D View - {3D}.png".
      // Get the first image (we only listed one view
      // in views).

      var files = Directory.GetFiles( 
        dir, $"{fn}*.*" );

      return files.Length > 0
        ? files[ 0 ]
        : string.Empty;
    }
  }
}

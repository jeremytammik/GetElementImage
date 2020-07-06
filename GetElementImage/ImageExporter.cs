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
    /// View points to export
    /// distance -- from element centre (defined by the bounding box).
    /// yaw -- horizontal angle, 0-360 degrees; 0 means looking North, 180 means looking South
    /// pitch -- vertical angle, -90 to +90 degrees, 0 being on the same height as the element centre and 90 looking from the top
    /// </summary>
    object[][] _view_data_to_export =
    {
      new object[] { "Isometric", 1, 45, 35.264 }, // 35.264
      new object[] { "North", 1.0, 0.0, 0.0 },
      new object[] { "East", 1.0 90.0, 0.0 },
      new object[] { "Top", 1, 0, 90, 0 }
    };

    List<View> _views_to_export;
    //List<ElementId> _category_ids_to_hide;

    /// <summary>
    /// Categories to hide
    /// </summary>
    BuiltInCategory[] _categories_to_hide
      = new BuiltInCategory[]
    {
      BuiltInCategory.OST_Cameras,
      BuiltInCategory.OST_IOS_GeoSite,
      BuiltInCategory.OST_Levels,
      BuiltInCategory.OST_ProjectBasePoint
    };

    ViewOrientation3D GetOrientationFor( 
      double yaw_degrees, 
      double pitch_degrees )
    {
      // Yaw = 0 means north; rotate by 90 degrees, 
      // so that north is 90 degrees instead

      if( Util.IsEqual( 270, yaw_degrees ) 
        || 270 > yaw_degrees )
      {
        yaw_degrees += 90.0;
      }

      double angle_in_xy_plane = yaw_degrees * Math.PI / 180.0;
      double angle_up_down = pitch_degrees * Math.PI / 180.0;

      // Eye position is arbitrary, since it is defined 
      // by fitting the view to the selected element

      XYZ eye = new XYZ(
        Math.Cos( angle_in_xy_plane ),
        Math.Sin( angle_in_xy_plane ),
        Math.Cos( angle_up_down ) );

      XYZ forward = -eye;

      XYZ left = Util.IsVertical( forward )
        ? -XYZ.BasisX
        : XYZ.BasisZ.CrossProduct( forward );

      XYZ up = forward.CrossProduct( left );

      // Setting ùp`to the Z axis, XYZ.BasisZ, throws
      // Autodesk.Revit.Exceptions.ArgumentsInconsistentException:
      // The vectors upDirection and forwardDirection 
      // are not perpendicular.

      ViewOrientation3D orientation
        = new ViewOrientation3D( eye, up, forward );

      return orientation;
    }

    public ImageExporter( Document doc )
    {
      ViewFamilyType viewFamilyType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( ViewFamilyType ) )
          .Cast<ViewFamilyType>()
          .FirstOrDefault( x =>
            x.ViewFamily == ViewFamily.ThreeDimensional );

      Debug.Assert( null != viewFamilyType );

      View3D v = View3D.CreateIsometric( 
        doc, viewFamilyType.Id );

      v.Name = "Isometric";

      Debug.Assert( null != v );

      int nViews = _view_data_to_export.Length;

      _views_to_export = new List<View>( nViews ) { v };

      for( int i = 1; i < nViews; ++i )
      {
        v = View3D.CreateIsometric(
          doc, viewFamilyType.Id );

        object[] d = _view_data_to_export[ i ];

        v.Name = d[ 0 ] as string;
        v.SetOrientation( GetOrientationFor( 
          (double)(d[ 2 ]), (double) (d[ 3 ]) ) );
        v.SaveOrientation();

        _views_to_export.Add( v );
      }

      foreach( View v2 in _views_to_export )
      {
        Parameter graphicDisplayOptions
          = v2.get_Parameter(
            BuiltInParameter.MODEL_GRAPHICS_STYLE );

        // Settings for best quality

        graphicDisplayOptions.Set( 6 );

        // Get categories to hide

        Categories cats = doc.Settings.Categories;

        foreach( BuiltInCategory bic in _categories_to_hide )
        {
          Category cat = cats.get_Item( bic );

          // OST_Cameras returns a null Category 
          // object in my model

          if( null == cat )
          {
            Debug.Print( "{0} returns null category.", bic );
          }
          else
          {
            v2.SetCategoryHidden( cat.Id, true );
          }

          // BuiltInCategory.OST_Cameras throws exception:
          // Autodesk.Revit.Exceptions.ArgumentException
          // Category cannot be hidden.
          // Parameter name: categoryId
          //ElementId id = new ElementId( bic );
          //view3d.SetCategoryHidden( id, true );
        }
      }
    }

    public string[] ExportToImage( Element e )
    {
      Document doc = e.Document;

      // Hide all other elements in views to export

      foreach( View v in _views_to_export )
      {

        List<ElementId> hideable_element_ids
          = new FilteredElementCollector( doc, v.Id )
            .Where<Element>( a => a.CanBeHidden( v ) )
            .Select<Element, ElementId>( b => b.Id )
            .ToList<ElementId>();

        v.HideElements( hideable_element_ids );

        List<ElementId> ids = new List<ElementId>( 1 ) { e.Id };

        v.UnhideElements( ids );
      }

      doc.Regenerate();

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

      try
      {
        doc.ExportImage( ieo );
      }
      catch(Exception ex)
      {
        Debug.Print( ex.Message );
      }

      // File name has format like 
      // "tempFileName - view type - view name", e.g.
      // "12345678 - 3D View - {3D}.png".
      // Get the first image (we only listed one view
      // in views).

      var files = Directory.GetFiles( 
        dir, $"{fn}*.*" );

      return files;
    }
  }
}

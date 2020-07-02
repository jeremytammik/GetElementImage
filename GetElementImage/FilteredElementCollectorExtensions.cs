#if NEED_THIS
#region Namespaces
using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace GetElementImage
{
  /// <summary>
  /// By Alexander Ignatovich, described in
  /// https://thebuildingcoder.typepad.com/blog/2013/08/setting-a-default-3d-view-orientation.html
  /// </summary>
  class FilteredElementCollectorExtensions
  {
    public static FilteredElementCollector OfClass<T>(
      this FilteredElementCollector collector )
        where T : Element
    {
      return collector.OfClass( typeof( T ) );
    }

    public static IEnumerable<T> OfType<T>(
      this FilteredElementCollector collector )
        where T : Element
    {
      return Enumerable.OfType<T>(
        source: collector.OfClass<T>() );
    }
  }
}
#endif
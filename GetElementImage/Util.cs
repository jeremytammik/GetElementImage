using Autodesk.Revit.DB;
using System;

namespace GetElementImage
{
  class Util
  {
    #region Geometrical Comparison
    public const double _eps = 1.0e-9;

    public static double Eps
    {
      get
      {
        return _eps;
      }
    }

    public static bool IsZero(
      double a,
      double tolerance = _eps )
    {
      return tolerance > Math.Abs( a );
    }

    public static bool IsEqual(
      double a,
      double b,
      double tolerance = _eps )
    {
      return IsZero( b - a, tolerance );
    }

    public static bool IsVertical( XYZ v )
    {
      return IsZero( v.X ) && IsZero( v.Y );
    }
    #endregion // Geometrical Comparison

  }
}

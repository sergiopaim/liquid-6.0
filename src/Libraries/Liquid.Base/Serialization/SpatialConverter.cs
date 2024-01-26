using GeoCoordinatePortable;
using Microsoft.Azure.Cosmos.Spatial;

namespace Liquid.Base
{
    /// <summary>
    /// Utility class to deal with CosmosDB Spatial data and converstion to external types (to use in ViewModels and in in-memory calculations)
    /// </summary>
    public static class SpatialConverter
    {
        /// <summary>
        /// Converts a Position value into a GeoCoordinate value
        /// </summary>
        /// <param name="position">Position to convert</param>
        /// <returns>GeoCoordinate value converted from position</returns>
        public static GeoCoordinate CoordinateFrom(Position position)
        {
            if (position is null)
                return new();
            else
                return new(position.Coordinates[1], position.Coordinates[0]);
        }

        /// <summary>
        /// Converts a Point value into a GeoCoordinate value
        /// </summary>
        /// <param name="point">Point to convert</param>
        /// <returns>GeoCoordinate value converted from point</returns>
        public static GeoCoordinate CoordinateFrom(Point point)
        {
            if (point is null)
                return new();
            else
                return CoordinateFrom(point.Position);
        }

        /// <summary>
        /// Converts a Point value into a Position value
        /// </summary>
        /// <param name="point">Point to convert</param>
        /// <returns>Position value converted from point</returns>
        public static Position PositionFrom(Point point)
        {
            if (point is null)
                return new(0, 0);
            else
                return point.Position;
        }

        /// <summary>
        /// Converts a Position value into a Point value
        /// </summary>
        /// <param name="position">Position to convert</param>
        /// <returns>Point value converted from position</returns>
        public static Point PointFrom(Position position)
        {
            if (position is null)
                return new(0, 0);
            else
                return new(position.Coordinates[0], position.Coordinates[1]);
        }
    }
}


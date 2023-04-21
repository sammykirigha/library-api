using System.Dynamic;
using System.Runtime.CompilerServices;

namespace CourseLibrary.API.Helpers
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(
            this IEnumerable<TSource> source,
            string? fields)
        {
         if(source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }


        }
    }
}

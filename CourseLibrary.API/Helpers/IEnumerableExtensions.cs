using System.Dynamic;
using System.Reflection;
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

            //create a list to hold our ExpandoObjects
            var expandoObjectList = new List<ExpandoObject>();

            //create a list with PropertyInfo objects onTSource. Rflection is expensive, so rather than doing it for 
            //object in the list, we do it once and reuse the results. after all, part of the reflection is on type of the object (TSource)
            //not on the instance

            var propertyInfoList = new List<PropertyInfo>();

            if(string.IsNullOrEmpty(fields))
            {
                //all public properties should be in the ExpandoObject
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                //the fields are separated by "," so we split it.
                var fieldsAfterSplit = fields.Split(',');

                foreach(var field in fieldsAfterSplit)
                {
                    //trim each field, as it might contain leading
                    //or trailing spaces. Can't trim the var in foreach,
                    //so use another var

                    var propertyName = field.Trim();

                    //use reflection to get the property on the source object
                    //we need to include public and instance, b/c specifying a binding
                    //flag overwrites the already-existing binding flags.
                    var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if(propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} wasn't found on" + $"{typeof(TSource).Name}");
                    }

                    //add propertyInfo to list
                    propertyInfoList.Add(propertyInfo);
                }
            }

            foreach(TSource sourceObject in source)
            {
                //create an ExpandoObject that will hold the
                //selected properties & values
                var dataShapedObject = new ExpandoObject();

                //Get the value of each property we have to return. For that,
                //we run through the list
                foreach(var propertyInfo in propertyInfoList)
                {
                    // GetValue returns the values of the property on the source object
                    var propertyValue = propertyInfo.GetValue(sourceObject);

                    //add the field to the ExpandoObject
                    ((IDictionary<string, object?>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                //add the ExpandoObject to the list
                expandoObjectList.Add(dataShapedObject);
            }
            return expandoObjectList;
        }
    }
}

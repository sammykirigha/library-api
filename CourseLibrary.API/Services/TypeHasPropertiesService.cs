using System.Reflection;

namespace CourseLibrary.API.Services
{
    public class TypeHasPropertiesService : ITypeHasPropertiesService
    {

        public bool TypeHasProperties<T>(string? fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            var fieldsAfterSplit = fields.Split(',');

            //the field are separated fields exists on source
            foreach (var field in fieldsAfterSplit)
            {
                var propertyName = field.Trim();
                var propertyInfo = typeof(T).GetProperty(propertyName,
                    BindingFlags.IgnoreCase |
                    BindingFlags.Public |
                    BindingFlags.Instance);

                //it cant be found return false
                if (propertyInfo == null)
                {
                    return false;
                }

            }
            // all checks out, return true
            return true;
        }
    }
}

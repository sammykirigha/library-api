using CourseLibrary.API.Services;

namespace CourseLibrary.API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(
            this IQueryable<T> source, 
            string orderBy, 
            Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if(source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if(mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(mappingDictionary));
            }

            if(string.IsNullOrEmpty(orderBy))
            {
                return source;
            }

            var orderByString = string.Empty;

            //the orderBy string is separeated by "," so we split it.
            var orderByAfterClause = orderBy.Split(",");

            //apply each orderBy clause
            foreach(var orderByclause in orderByAfterClause)
            {
                //trim the orderBy clause, as it might contain leading or trailing spaces. Can't trim the var in foreach, so we use another var
                var trimmedOrderByClause = orderByclause.Trim();

                //if the sort option ends with "desc" we, order descending, otherwise ascending
                var orderDescending = trimmedOrderByClause.EndsWith("desc");

                //remove "asc" or "desc" from the orderBy clause, so we get the property name to look for in the mapping dictionary
                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ? trimmedOrderByClause : trimmedOrderByClause.Remove(indexOfFirstSpace);

                //find the matching property
                if(!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentNullException($"Key mapping for {propertyName} is missing");
                }

                //get the propertyMappingValue
                var propertyMappingValue = mappingDictionary[propertyName];

                if(propertyMappingValue == null)
                {
                    throw new ArgumentNullException(nameof(propertyMappingValue));
                }

                if(propertyMappingValue.Revert)
                {
                    orderDescending = !orderDescending;
                }

                //Run through the property names
                foreach(var destinationProperty in propertyMappingValue.DestinationProperties)
                {
                    orderByString = orderByString + (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ", ")
                           + destinationProperty
                           + (orderDescending ? "descending" : "ascending");
                }
            }
           

        }
    }
}

namespace CourseLibrary.API.Services
{
    public interface ITypeHasPropertiesService
    {
        bool TypeHasProperties<T>(string? fields);
    }
}
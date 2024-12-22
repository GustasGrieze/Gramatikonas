namespace gramatikonas.Exceptions
{
    public class TaskUploadException : Exception
    {
        public TaskUploadException(string message) : base(message) { }
        public TaskUploadException(string message, Exception innerException) : base(message, innerException) { }
    }
}

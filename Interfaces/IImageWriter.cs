public interface IImageWriter
{
    Task WriteImage(string imageUrl, DateTime day, byte[] image);
}
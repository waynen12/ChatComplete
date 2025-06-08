public class ImageElement : IImageElement
{
    public string AltText { get; set; }
    public string ImagePath { get; set; }

    public string ElementType => "image";

    public ImageElement(string altText, string imagePath)
    {
        AltText = altText;
        ImagePath = imagePath;
    }
}

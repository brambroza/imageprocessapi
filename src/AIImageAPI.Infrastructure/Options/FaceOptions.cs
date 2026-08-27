namespace AIImageAPI.Infrastructure.Options;

public class FaceOptions
{
    public const string SectionName = "Face";

    public string ModelsDirectory { get; set; } = "models";
    public string DetectorModel { get; set; } = "scrfd_500m.onnx";
    public string EmbedderModel { get; set; } = "arcface_r100.onnx";
    public string AgeGenderModel { get; set; } = "genderage.onnx";

    /// <summary>
    /// Cosine similarity threshold for accepting a face match. 0.5 is a common ArcFace default.
    /// </summary>
    public float CosineThreshold { get; set; } = 0.5f;

    public string EmbeddingModelTag { get; set; } = "buffalo_l";
    public int EmbeddingDimension { get; set; } = 512;
}

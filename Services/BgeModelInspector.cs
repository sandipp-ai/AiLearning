using Microsoft.ML.OnnxRuntime;

namespace AiLearning.Console.Services;

public static class BgeModelInspector
{
    public static void Inspect()
    {
        string modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "Models",
            "BgeSmallEnV15",
            "model.onnx");

        using var session = new InferenceSession(modelPath);

        System.Console.WriteLine("ONNX Inputs:");

        foreach (var input in session.InputMetadata)
        {
            System.Console.WriteLine(
                $"{input.Key}: [{string.Join(", ", input.Value.Dimensions)}]");
        }

        System.Console.WriteLine("ONNX Outputs:");

        foreach (var output in session.OutputMetadata)
        {
            System.Console.WriteLine(
                $"{output.Key}: [{string.Join(", ", output.Value.Dimensions)}]");
        }
    }
}
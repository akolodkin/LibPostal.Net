namespace LibPostal.Net.Parser;

/// <summary>
/// Address parser model types.
/// Based on libpostal's model types.
/// </summary>
public enum ModelType
{
    /// <summary>
    /// Conditional Random Field (CRF) model.
    /// </summary>
    CRF = 0,

    /// <summary>
    /// Averaged Perceptron model.
    /// </summary>
    AveragedPerceptron = 1
}

using System.Text;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Domain.Profiles;

namespace ContentIntelligencePlatform.Application.Common;

/// <summary>
/// Compone el prompt final: Base + cadena de herencia de perfil + fuente (sección 11 del PRD).
/// Nunca modifica hechos, solo narrativa (principio de sección 10).
/// </summary>
public static class PromptBuilder
{
    public static string BuildSystemPrompt(string baseKnowledge, IReadOnlyList<KnowledgeProfileDocument> profileChain)
    {
        var sb = new StringBuilder();
        sb.AppendLine(baseKnowledge.Trim());
        sb.AppendLine();

        foreach (var profile in profileChain) // raíz -> hoja, la hoja tiene la última palabra
        {
            sb.AppendLine($"## Perfil: {profile.Name}");
            sb.AppendLine(profile.ContentMarkdown.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("Reglas estrictas: nunca inventes hechos que no estén en la fuente. Si falta información, decláralo explícitamente.");
        return sb.ToString();
    }

    public static string BuildExtractionUserPrompt(string sourceText) =>
        $"""
         Analiza el siguiente texto y extrae: título, resumen, lista de hechos verificables, nivel de confianza (0.0-1.0) e información faltante.
         Responde ÚNICAMENTE con JSON válido con las claves: title, summary, facts (array), confidence (number), missingInformation (string o null).

         TEXTO:
         {sourceText}
         """;

    public static string BuildScriptUserPrompt(string outputFormat, ExtractionResultDto extraction) =>
        $"""
         Con base en los siguientes hechos, redacta un guion para {outputFormat}.
         Responde ÚNICAMENTE con JSON válido con las claves: title, hook, introduction, body, ending, cta, hashtags (array), keywords (array), category, estimatedDurationSeconds (number), confidence (number 0-1), missingInformation (string o null), sources (array).

         HECHOS:
         {string.Join("\n- ", extraction.Facts)}

         RESUMEN: {extraction.Summary}

         INFORMACION FALTANTE DETECTADA EN LA ETAPA DE EXTRACCION: {extraction.MissingInformation ?? "(ninguna)"}
         Si esta informacion faltante sigue siendo relevante para el guion final, repórtala en tu propio campo "missingInformation" (puedes reformularla o combinarla con nuevos vacíos que detectes). No la ignores ni la des por resuelta sin justificación. Tu "confidence" debe reflejar este vacío si sigue vigente: no puede ser mayor que {extraction.Confidence} salvo que expliques por qué el vacío ya no aplica.
         """;
}

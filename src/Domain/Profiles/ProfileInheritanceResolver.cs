using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.Profiles;

/// <summary>
/// Resuelve la cadena de herencia de un perfil (sección 10 del PRD) y detecta ciclos.
/// </summary>
public class ProfileInheritanceResolver
{
    public Result<IReadOnlyList<Profile>> ResolveChain(Profile leaf, IReadOnlyDictionary<string, Profile> profilesBySlug)
    {
        var chain = new List<Profile> { leaf };
        var visited = new HashSet<string> { leaf.Slug };
        var current = leaf;

        while (current.ParentSlug is not null)
        {
            if (!visited.Add(current.ParentSlug))
                return Result.Failure<IReadOnlyList<Profile>>($"Ciclo de herencia detectado en el perfil '{current.ParentSlug}'.");

            if (!profilesBySlug.TryGetValue(current.ParentSlug, out var parent))
                return Result.Failure<IReadOnlyList<Profile>>($"El perfil padre '{current.ParentSlug}' no existe.");

            chain.Add(parent);
            current = parent;
        }

        // Orden: raíz primero, hoja al final (para aplicar overrides en ese orden en el Prompt Builder)
        chain.Reverse();
        return Result.Success<IReadOnlyList<Profile>>(chain);
    }
}

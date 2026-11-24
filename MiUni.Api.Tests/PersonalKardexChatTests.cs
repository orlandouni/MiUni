using MiUni.Api.Enums;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class PersonalKardexChatTests
{
    private static KardexMateria Row(Guid owner, string subject, EstadoMateria estado, decimal? cal, short cr) => new()
    { Id = Guid.NewGuid(), UsuarioId = owner, Materia = subject, Estado = estado, Calificacion = cal, Creditos = cr };

    [Fact]
    public async Task AnswersOwnKardexAndLeavesOtherQuestionsToRag()
    {
        await using var db = ChatContextTests.Database(); var user = Guid.NewGuid();
        db.KardexMaterias.AddRange(Row(user, "Calculo I", EstadoMateria.Aprobada, 90, 10),
            Row(user, "Fisica I", EstadoMateria.Reprobada, 50, 8),
            Row(user, "Programacion", EstadoMateria.Cursando, null, 9),
            Row(Guid.NewGuid(), "FOREIGN", EstadoMateria.Aprobada, 100, 10));
        await db.SaveChangesAsync();

        var creditos = await PersonalKardex.Resolve(db, user, "¿Cuántos créditos tengo?", default);
        Assert.Contains("Créditos aprobados: 10", creditos); Assert.Contains("Promedio: 70", creditos);
        Assert.DoesNotContain("FOREIGN", creditos);
        Assert.Contains("Fisica I", await PersonalKardex.Resolve(db, user, "que materias reprobe", default));
        Assert.Contains("Programacion", await PersonalKardex.Resolve(db, user, "que materias llevo este semestre", default));
        Assert.NotNull(await PersonalKardex.Resolve(db, user, "en mi kardex lo subi", default));
        Assert.Null(await PersonalKardex.Resolve(db, user, "como descargo mi kardex", default));
        Assert.Null(await PersonalKardex.Resolve(db, user, "Donde esta la biblioteca?", default));
        Assert.Contains("No tienes materias", await PersonalKardex.Resolve(db, Guid.NewGuid(), "mi promedio", default));
    }
}

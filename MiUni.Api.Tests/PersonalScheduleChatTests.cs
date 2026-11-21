using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class PersonalScheduleChatTests
{
    private static readonly DateTime SundayNight = new(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc); // Sunday 23:30 campus.
    private static HorarioClase Row(Guid owner, string subject, short day, int start, int end) => new()
    { Id = Guid.NewGuid(), UsuarioId = owner, Materia = subject, Dia = day, HoraInicio = new(start, 0), HoraFin = new(end, 0) };

    [Fact]
    public async Task SundayTomorrowAndNextClassUseCampusTimeAndFreshOwnedRows()
    {
        await using var db = ChatContextTests.Database(); var user = Guid.NewGuid(); var other = Guid.NewGuid();
        var row = Row(user, "Monday saved class", 1, 9, 10);
        db.HorarioClases.AddRange(row, Row(other, "FOREIGN", 1, 8, 9)); await db.SaveChangesAsync();
        var tomorrow = await PersonalSchedule.Resolve(db, user, "Que clase tengo mañana?", SundayNight, default);
        Assert.Contains("Monday saved class", tomorrow.Answer); Assert.DoesNotContain("FOREIGN", tomorrow.Answer);
        Assert.Contains("salón no registrado", tomorrow.Answer); Assert.Contains("profesor no registrado", tomorrow.Answer);
        Assert.Contains("2026-11-23", (await PersonalSchedule.Resolve(db, user, "Mi proxima clase?", SundayNight, default)).Answer);
        row.Materia = "Fresh edited class"; await db.SaveChangesAsync();
        Assert.Contains("Fresh edited class", (await PersonalSchedule.Resolve(db, user, "Mi horario", SundayNight, default)).Answer);
        Assert.Contains("No hay clases", (await PersonalSchedule.Resolve(db, user, "Que clase tengo el martes?", SundayNight, default)).Answer);
        Assert.Contains("No tienes un horario", (await PersonalSchedule.Resolve(db, Guid.NewGuid(), "Mi horario", SundayNight, default)).Answer);
    }

    [Theory]
    [InlineData(16, "First", true)] // Monday 09:00 campus: inclusive start.
    [InlineData(17, "First", false)] // Monday 10:00 campus: exclusive end.
    public async Task CurrentClassBoundsPreserveOverlaps(int utcHour, string subject, bool included)
    {
        await using var db = ChatContextTests.Database(); var user = Guid.NewGuid();
        db.HorarioClases.AddRange(Row(user, "First", 1, 9, 10), Row(user, "Overlap", 1, 9, 11));
        await db.SaveChangesAsync();
        var result = await PersonalSchedule.Resolve(db, user, "Que clase tengo ahora?",
            new DateTime(2026, 11, 23, utcHour, 0, 0, DateTimeKind.Utc), default);
        Assert.Equal(included, result.Answer!.Contains(subject)); Assert.Contains("Overlap", result.Answer);
    }

    [Fact]
    public async Task MixedQueriesGetOnlySelectedFieldsAndInstitutionalHoursDoNotUsePrivateSchedule()
    {
        await using var db = ChatContextTests.Database(); var user = Guid.NewGuid();
        db.HorarioClases.AddRange(Row(user, "Monday", 1, 9, 10), Row(user, "Tuesday", 2, 11, 12));
        await db.SaveChangesAsync();
        var mixed = await PersonalSchedule.Resolve(db, user, "Mi horario mañana y documentos para practicas", SundayNight, default);
        Assert.Null(mixed.Answer); Assert.Equal("Monday", Assert.Single(mixed.Context!.Schedule).Subject);
        Assert.Null((await PersonalSchedule.Resolve(db, user, "Horario de Rectoria", SundayNight, default)).Context);
        Assert.Contains("sin periodos", (await PersonalSchedule.Resolve(db, user, "Mi horario semestre pasado", SundayNight, default)).Answer);
        Assert.Contains("sin periodos", (await PersonalSchedule.Resolve(db, user, "Mi horario en festivo", SundayNight, default)).Answer);
    }
}

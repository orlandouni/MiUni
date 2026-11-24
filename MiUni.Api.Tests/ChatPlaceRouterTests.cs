using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class ChatPlaceRouterTests
{
    [Theory]
    [InlineData("Que lugares hay")]
    [InlineData("Que lugares hay en el campus")]
    public void ListsCatalogBeforeBuildingTheProfilePrompt(string question)
    {
        var places = new[] { new ChatPlace("Biblioteca Central", "Bibliotecas"), new ChatPlace("Comedor Universitario", "Comida") };
        var answer = ChatPlaceRouter.TryBuildAnswer(question, places);
        Assert.NotNull(answer);
        Assert.Contains("Biblioteca Central", answer);
        Assert.Contains("Comedor Universitario", answer);
    }

    [Fact]
    public void MixedOpeningHoursQuestionKeepsDocumentaryFallback()
    {
        var places = new[] { new ChatPlace("Biblioteca Central", "Bibliotecas") };
        Assert.Null(ChatPlaceRouter.TryBuildAnswer(
            "Donde esta Biblioteca Central y a que hora abre", places));
    }

    [Theory]
    [InlineData("¿Dónde está la Biblioteca Central?")]
    [InlineData("dame la ubicacion de biblioteca central")]
    [InlineData("como llego a la biblioteca central")]
    public void BuildsFastAnswerForAnExactCatalogPlace(string question)
    {
        var places = new[] { new ChatPlace("Biblioteca Central", "Bibliotecas") };

        var answer = ChatPlaceRouter.TryBuildAnswer(question, places);

        Assert.Equal(
            "Encontré Biblioteca Central. Abre el enlace debajo de esta respuesta para verla en el mapa del campus.",
            answer);
    }

    [Fact]
    public void DoesNotRouteGeneralQuestionsOrUnknownPlaces()
    {
        var places = new[] { new ChatPlace("Biblioteca Central", "Bibliotecas") };

        Assert.Null(ChatPlaceRouter.TryBuildAnswer("Hola, ¿cómo estás?", places));
        Assert.Null(ChatPlaceRouter.TryBuildAnswer("¿Dónde está Rectoría?", places));
    }

    [Fact]
    public void MatchesSeveralExactPlaces()
    {
        var places = new[]
        {
            new ChatPlace("Biblioteca Central", "Bibliotecas"),
            new ChatPlace("Comedor Universitario", "Comida"),
        };

        var answer = ChatPlaceRouter.TryBuildAnswer(
            "¿Dónde están la Biblioteca Central y el Comedor Universitario?", places);

        Assert.Contains("Biblioteca Central", answer);
        Assert.Contains("Comedor Universitario", answer);
    }
}

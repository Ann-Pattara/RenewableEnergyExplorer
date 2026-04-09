using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RenewableEnergyAPI.Controllers;
using RenewableEnergyAPI.Models;
using RenewableEnergyAPI.Services;
using RenewableEnergyContracts;

namespace API.Tests;

public class EnergyControllerTests
{
    private readonly Mock<IWorldBankService> _serviceMock = new();
    private readonly EnergyController _controller;

    public EnergyControllerTests()
    {
        var logger = Mock.Of<ILogger<EnergyController>>();
        _controller = new EnergyController(_serviceMock.Object, logger);
    }

    [Fact]
    public async Task Search_InvalidTopic_Returns400()
    {
        var query = new EnergySearchQuery { Topic = "nuclear" };

        var result = await _controller.Search(query);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(body.Success);
        Assert.Contains("Invalid topic", body.Message);
    }

    [Fact]
    public async Task Search_InvalidStartDateFormat_Returns400()
    {
        var query = new EnergySearchQuery { Topic = "all", StartDate = "13-2024-01" };

        var result = await _controller.Search(query);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(body.Success);
        Assert.Contains("StartDate", body.Message);
    }

    [Fact]
    public async Task Search_ValidQuery_Returns200WithData()
    {
        var docs = new List<EnergyDocument>
        {
            new() { Id = "1", Title = "Wind Power Report", Topic = "wind" }
        };
        _serviceMock
            .Setup(s => s.SearchAsync(It.IsAny<EnergySearchQuery>()))
            .ReturnsAsync((docs, 1));

        var query = new EnergySearchQuery { Topic = "wind" };
        var result = await _controller.Search(query);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiResponse<List<EnergyDocument>>>(ok.Value);
        Assert.True(body.Success);
        Assert.Single(body.Data!);
        Assert.Equal("Wind Power Report", body.Data![0].Title);
    }

    [Fact]
    public async Task Search_ServiceThrowsHttpException_Returns502()
    {
        _serviceMock
            .Setup(s => s.SearchAsync(It.IsAny<EnergySearchQuery>()))
            .ThrowsAsync(new HttpRequestException("upstream failure"));

        var query = new EnergySearchQuery { Topic = "all" };
        var result = await _controller.Search(query);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
        var body = Assert.IsType<ApiResponse<object>>(obj.Value);
        Assert.False(body.Success);
    }
}

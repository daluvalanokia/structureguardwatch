using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StructureWatch.Web.Controllers;
using StructureWatch.Core.Services;
using Moq;

namespace StructureWatch.Web.Tests;

public class MapControllerTests
{
    [Fact]
    public void Index_ReturnsView()
    {
        var overpass = new Mock<IOverpassService>();
        var logger = new Mock<ILogger<MapController>>();
        var controller = new MapController(overpass.Object, logger.Object);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Footprints_ReturnsBadRequest_ForInvalidBbox()
    {
        var overpass = new Mock<IOverpassService>();
        var logger = new Mock<ILogger<MapController>>();
        var controller = new MapController(overpass.Object, logger.Object);

        var result = await controller.Footprints("invalid");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Footprints_ReturnsBadRequest_ForMissingParts()
    {
        var overpass = new Mock<IOverpassService>();
        var logger = new Mock<ILogger<MapController>>();
        var controller = new MapController(overpass.Object, logger.Object);

        var result = await controller.Footprints("40.75,-73.98");

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

﻿using System.Linq;
using System.Threading.Tasks;
using NSwag.SwaggerGeneration.AspNetCore.Tests.Web.Controllers.Parameters;
using Xunit;

namespace NSwag.SwaggerGeneration.AspNetCore.Tests.Parameters
{
    public class BodyParametersTests : AspNetCoreTestsBase
    {
        [Fact]
        public async Task When_body_parameter_has_no_default_value_then_it_is_required()
        {
            // Arrange
            var settings = new AspNetCoreToSwaggerGeneratorSettings();

            // Act
            var document = await GenerateDocumentAsync(settings, typeof(BodyParametersController));

            // Assert
            var operation = document.Operations.First(o => o.Operation.OperationId == "BodyParameters_Required").Operation;

            Assert.True(operation.ActualParameters.First().IsRequired);
        }

        [Fact]
        public async Task When_body_parameter_has_default_value_then_it_is_optional()
        {
            // Arrange
            var settings = new AspNetCoreToSwaggerGeneratorSettings();

            // Act
            var document = await GenerateDocumentAsync(settings, typeof(BodyParametersController));

            // Assert
            var operation = document.Operations.First(o => o.Operation.OperationId == "BodyParameters_Optional").Operation;

            Assert.False(operation.ActualParameters.First().IsRequired);
        }
    }
}
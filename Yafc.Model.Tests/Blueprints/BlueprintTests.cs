using Xunit;
using Yafc.Blueprints;

namespace Yafc.Model.Tests.Blueprints;

public class BlueprintTests {
    [Fact]
    public void ReadBlueprint() {
        var blueprintString = BlueprintString.FromBpString("0eNrVVctugzAQ/Jc9m4qXw0Pql1RRZIibWAWb2CZqFPHvXSAhTUNTiHpoxQWW9cx4dtc+QlbUvNJCWkiPIHIlDaQvRzBiI1nRxiQrOaTAjOFlVgi5cUqWb4XkTgANASHX/B1Sr1kS4NIKK3iP0H0cVrIuM64xgZyRKoQSe+5UWu3Fmmsn33JjgUClDC5XsmVFyCD2nyiBA6S+i2/IlbEWyG3IDbo/oGd18eYIabi2+OMuaNCBroXmeZ8RjyAHD+imY7rDEfBwumw6TzYdkDXf1ajznt7wi97TktWrKHCdafNMT9aX9lxzAkPGVfTELLSSTlUwy5F0V7MCBWJYKl1iaxHIVVkxzaxCvfDcBeq2DyPaLPEZ2dViul/hlV8jWNEch6K/5VDQlfwEd+n9SlS/ZnU83eroJ6uTB2Yonjz7njtdajxvijxvRpMk7n8ZI2/6cflpU98U17uckKN3xEgVggGTnozK284dGrj/Xt3sdYw9nM2+uMfuWOVsNBq4nqmDztaRPOoC3rTC8rKt3nB5E9hjA3XwdOEnYZJQmkSR70VN8wGue5U0");
        var blueprint = blueprintString.blueprint;
        
        Assert.Equal(Blueprint.VERSION, blueprint.version);
    }
}

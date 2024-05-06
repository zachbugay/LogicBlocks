namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

public class PropertyMetadataTest {
  [Fact]
  public void Initializes() {
    var property = new PropertyMetadata(
      Name: "Name",
      Getter: _ => "Value",
      Setter: (_, _) => { },
      GenericType: new GenericType(typeof(string), typeof(string), [], _ => { }, _ => { }),
      Attributes: new Dictionary<Type, Attribute[]>()
    );

    property.ShouldBeOfType<PropertyMetadata>();
  }
}

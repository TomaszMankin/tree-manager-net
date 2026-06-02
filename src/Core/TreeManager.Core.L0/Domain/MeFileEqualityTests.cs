using System;
using System.Collections.Generic;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class MeFileEqualityTests
{
    #region Equals

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Equals_ReturnsTrue_WhenTwoInstancesHaveIdenticalContent()
    {
        //Arrange
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var a = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Jan",
            LastName = "Kowalski",
            PersonName = "Jan Kowalski",
            Parents = new List<string> { "Anna Nowak" },
            ParentsId = new List<Guid> { parentId },
            Notes = "Notatka",
        };

        var b = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Jan",
            LastName = "Kowalski",
            PersonName = "Jan Kowalski",
            Parents = new List<string> { "Anna Nowak" },
            ParentsId = new List<Guid> { parentId },
            Notes = "Notatka",
        };

        //Act
        var result = a.Equals(b);

        //Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("OtherFirstNames")]
    [InlineData("Notes")]
    [InlineData("DatesOfBirth")]
    [InlineData("DatesOfDeath")]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Equals_ReturnsFalse_WhenAnyScalarFieldDiffers(string fieldName)
    {
        //Arrange
        var id = Guid.NewGuid();
        var a = new MeFile { UniqueIdentifier = id, FirstName = "Jan", LastName = "Kowalski" };

        var b = fieldName switch
        {
            "FirstName"       => a with { FirstName = "Piotr" },
            "LastName"        => a with { LastName = "Wiśniewski" },
            "OtherFirstNames" => a with { OtherFirstNames = "Władysław" },
            "Notes"           => a with { Notes = "zmieniona notatka" },
            "DatesOfBirth"    => a with { DatesOfBirth = "1|1|1900" },
            "DatesOfDeath"    => a with { DatesOfDeath = "1|1|2000" },
            _                 => throw new ArgumentOutOfRangeException(fieldName),
        };

        //Act
        var result = a.Equals(b);

        //Assert
        Assert.False(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Equals_ReturnsFalse_WhenAListMemberDiffers()
    {
        //Arrange
        var id = Guid.NewGuid();
        var a = new MeFile
        {
            UniqueIdentifier = id,
            Children = new List<string> { "Marek Kowalski" },
            ChildrenId = new List<Guid> { Guid.NewGuid() },
        };

        var b = new MeFile
        {
            UniqueIdentifier = id,
            Children = new List<string> { "Inny Kowalski" },
            ChildrenId = a.ChildrenId,
        };

        //Act
        var result = a.Equals(b);

        //Assert
        Assert.False(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Equals_ReturnsFalse_WhenListOrderDiffers()
    {
        //Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var a = new MeFile
        {
            Siblings = new List<string> { "Adam", "Ewa" },
            SiblingsId = new List<Guid> { id1, id2 },
        };

        var b = new MeFile
        {
            Siblings = new List<string> { "Ewa", "Adam" },
            SiblingsId = new List<Guid> { id2, id1 },
        };

        //Act
        var result = a.Equals(b);

        //Assert
        Assert.False(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Equals_IgnoresLocation_WhenOnlyLocationDiffers()
    {
        //Arrange
        var id = Guid.NewGuid();

        var a = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Jan",
            Location = @"C:\machine-a\Lista osób\Jan Kowalski",
        };

        var b = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Jan",
            Location = @"D:\machine-b\Lista osób\Jan Kowalski",
        };

        //Act
        var result = a.Equals(b);

        //Assert
        Assert.True(result);
    }

    #endregion

    #region GetHashCode

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetHashCode_ReturnsSameValue_WhenContentIdentical()
    {
        //Arrange
        var id = Guid.NewGuid();
        var spouseId = Guid.NewGuid();

        var a = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Maria",
            LastName = "Nowak",
            Spouse = new List<string> { "Jan Nowak" },
            SpouseId = new List<Guid> { spouseId },
        };

        var b = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Maria",
            LastName = "Nowak",
            Spouse = new List<string> { "Jan Nowak" },
            SpouseId = new List<Guid> { spouseId },
        };

        //Act + Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    #endregion
}

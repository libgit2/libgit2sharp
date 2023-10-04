using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibGit2Sharp.Tests;
public class ExceptionTests
{
    [Fact]
    public void When_CreatingException_Given_ItHasErrorCode_Then_ErrorCodeIsSet()
    {
        var instance = new LibGit2SharpException("message").WithErrorCode(Core.GitErrorCode.Certificate, Core.GitErrorCategory.Ssh);
        Assert.Equal(Core.GitErrorCategory.Ssh, instance.GitErrorCategory);
        Assert.Equal(Core.GitErrorCode.Certificate, instance.GitErrorCode);
    }
}

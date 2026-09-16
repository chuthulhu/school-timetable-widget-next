using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

namespace SchoolTimetableWidget.Tests.Persistence;

public class ProfileRecoveryFilesTests
{
    [Fact]
    public void PendingMarkerAndValidatedPreviousRevisionSurviveRestart()
    {
        using var temp = new TempProfile();
        var files = new ProfileRecoveryFiles(temp.Directory);
        Assert.False(files.IsPending);
        files.Secure(ProfileBackupFileTests.Sample());
        File.WriteAllBytes(temp.File, ProfileJson.Serialize(ProfileSnapshot.Defaults()));
        var restarted = new ProfileRecoveryFiles(temp.Directory);
        Assert.True(restarted.IsPending);
        Assert.Equal(ProfileJson.Serialize(ProfileBackupFileTests.Sample()), ProfileJson.Serialize(restarted.ReadPrevious()));
        Assert.NotEqual(File.ReadAllBytes(temp.File), File.ReadAllBytes(files.SnapshotPath));
    }

    [Fact]
    public void PendingRecoveryCannotOverwriteLastKnownGood()
    {
        using var temp = new TempProfile(); var files = new ProfileRecoveryFiles(temp.Directory);
        files.Secure(ProfileBackupFileTests.Sample());
        var snapshot = File.ReadAllBytes(files.SnapshotPath); var marker = File.ReadAllBytes(files.MarkerPath);
        Assert.Throws<IOException>(() => files.Secure(ProfileSnapshot.Defaults()));
        Assert.Equal(snapshot, File.ReadAllBytes(files.SnapshotPath)); Assert.Equal(marker, File.ReadAllBytes(files.MarkerPath));
    }

    [Theory]
    [InlineData("missing")] [InlineData("invalid")] [InlineData("different-valid")] [InlineData("marker-invalid")]
    public void DamagedRecoveryEvidenceIsNeverSilentlyAcceptedOrCleaned(string damage)
    {
        using var temp = new TempProfile(); var files = new ProfileRecoveryFiles(temp.Directory);
        files.Secure(ProfileBackupFileTests.Sample());
        switch (damage)
        {
            case "missing": File.Delete(files.SnapshotPath); break;
            case "invalid": File.WriteAllText(files.SnapshotPath, "{"); break;
            case "different-valid": File.WriteAllBytes(files.SnapshotPath, ProfileJson.Serialize(ProfileSnapshot.Defaults())); break;
            case "marker-invalid": File.WriteAllText(files.MarkerPath, "{"); break;
        }
        var marker = File.ReadAllBytes(files.MarkerPath);
        Assert.NotNull(Record.Exception(() => files.ReadPrevious()));
        Assert.NotNull(Record.Exception(() => files.Complete()));
        Assert.True(files.IsPending); Assert.Equal(marker, File.ReadAllBytes(files.MarkerPath));
    }

    [Fact]
    public void CompletionKeepsOnePreviousSnapshotAndAllowsNextRevision()
    {
        using var temp = new TempProfile(); var files = new ProfileRecoveryFiles(temp.Directory);
        files.Secure(ProfileBackupFileTests.Sample()); files.Complete();
        Assert.False(files.IsPending); Assert.True(File.Exists(files.SnapshotPath));
        files.Secure(ProfileSnapshot.Defaults());
        Assert.Equal(ProfileJson.Serialize(ProfileSnapshot.Defaults()), ProfileJson.Serialize(files.ReadPrevious()));
    }

    [Fact]
    public void SnapshotWriteFailureDoesNotCreateMarkerOrChangeCurrentProfile()
    {
        using var temp = new TempProfile(); var files = new ProfileRecoveryFiles(temp.Directory);
        var original = ProfileJson.Serialize(ProfileBackupFileTests.Sample()); File.WriteAllBytes(temp.File, original);
        Directory.CreateDirectory(files.SnapshotPath);
        Assert.NotNull(Record.Exception(() => files.Secure(ProfileSnapshot.Defaults())));
        Assert.False(files.IsPending); Assert.Equal(original, File.ReadAllBytes(temp.File));
    }
}

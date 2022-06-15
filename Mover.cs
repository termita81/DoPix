using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;

class Mover
{
	const double PanoramaRatio = 1.6;
	private const string Jpeg = "jpeg";
	private const string Jpg = "jpg";
	private const string Mp4 = "mp4";
	private readonly List<string> photoExtensions = new List<string> { Jpeg, Jpg };
	private readonly List<string> movieExtensions = new List<string> { Mp4 };

	string fullFilePath = "";
	string directory = "";
	string newDirectory = "";
	string filename = "";
	string newFilename = "";
	string extension = "";
	bool isPanorama = false;
	bool doIt = false;
    private readonly bool overwrite;
    private readonly VerbosityLevel verbosityLevel;
    DateTime? shootingDate;

	public Mover(bool doIt, bool overwrite, VerbosityLevel verbosityLevel)
    {
        this.doIt = doIt;
        this.overwrite = overwrite;
        this.verbosityLevel = verbosityLevel;
    }

	// I think this would rename folders based on photo dates inside them
    public void UpdateFoldersNamesByPhotoDates(string root)
	{
		foreach (var folder in Directory.EnumerateDirectories(root, "20??.?? *", SearchOption.TopDirectoryOnly))
		{
			Console.WriteLine(folder);
			string newFolder = null;
			var folderName = Path.GetFileName(folder);
			foreach (var file in Directory.EnumerateFiles(folder))
			{
				fullFilePath = file;
				filename = Path.GetFileNameWithoutExtension(file).Trim();
				var dateTime = ExtractDateFromPhoto();
				if (dateTime != null && dateTime != DateTime.MinValue)
				{
					newFolder = Path.Combine(root, $"{dateTime.Value.ToString("yyyy.MM.dd")} {folderName.Substring(7)}");
					Console.WriteLine($"Found '{newFolder}'");
					break;
				}
			}
			if (newFolder != null)
			{
				var regex = new Regex(@"\s{2,}");
				while (regex.IsMatch(newFolder))
				{
					newFolder = regex.Replace(newFolder, " ");
				}
				Directory.Move(folder, newFolder);
			}
			else
			{
				Console.WriteLine($"Could not find date for '{folder}'");
			}
		}
	}

	public void SplitFilesToFoldersByDate(string root)
	{
		if (!Directory.Exists(root))
        {
			Console.WriteLine($"'{root}' not found.");
			return;
        }

		Console.WriteLine($"'{root}': {(doIt ? "Actually moving files" : "NOT moving files")}");
		var files = Directory.EnumerateFiles(root);
		foreach (var file in files) // "c:\work\pix\nesortate\Moto\mici\IMG_20170315_132557065.jpg"
		{
			try
			{
				fullFilePath = file;	
				directory = Path.GetDirectoryName(fullFilePath);
				filename = Path.GetFileName(fullFilePath); // "IMG_20170315_132557065.jpg"
				extension = Path.GetExtension(filename).ToLowerInvariant().Substring(1);

				shootingDate = ExtractDateFromFile();
				if (shootingDate == null)
				{
					Console.WriteLine($"  Skipping '{fullFilePath}': could not retrieve shooting date.");
					continue;
				}

				MakeSureDirExists();

				newFilename = Path.Combine(newDirectory, filename);

				MoveFile(fullFilePath, newFilename);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"  Exception for '{fullFilePath}'\n'{ex.Message}'\n{ex.StackTrace}");
			}
		}
	}

	private DateTime? ExtractDateFromFile()
	{
		if (photoExtensions.Contains(extension))
			return ExtractDateFromPhoto();
		if (movieExtensions.Contains(extension))
			return ExtractDateFromMovie();
		return null;
	}

	private DateTime? ExtractDateFromMovie()
	{
		return GetShootingDateFromName();
	}

	private DateTime? ExtractDateFromPhoto()
	{
		var result = GetShootingDateFromName();
		if (result == null)
		{
			try
			{
				var fileContent = File.ReadAllBytes(fullFilePath);
				var image = Image.Load(fileContent);
				var profile = image.Metadata.ExifProfile;
				isPanorama = IsPanorama(profile);
				result = GetShootingDateFromExif(profile);
			}
			catch (Exception exc)
			{
				return null;
			}
		}
		return result;
	}

	private DateTime? GetShootingDateFromName()
	{
		//var yearRegex = new Regex("^.*(?<a>20[\\d]{6})_?");
		//var match = yearRegex.Match(filename);
		//if (match.Success)
		//{
		//	var dateString = match.Groups["a"].Value;
		//	return new DateTime(Int32.Parse(dateString.Substring(0, 4))
		//	, Int32.Parse(dateString.Substring(4, 2))
		//	, Int32.Parse(dateString.Substring(6, 2)));
		//}
		return null;
	}

	private DateTime? GetShootingDateFromExif(ExifProfile exifProfile)
	{
		var date = exifProfile.Values.FirstOrDefault(e => e.Tag == ExifTag.DateTimeOriginal);
		// System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures).First() .Select(e => new { Df = e.DateTimeFormat, N = e.DisplayName, D = e.EnglishName, Na = e.Name })
		if (date != null) // BLEAH
		{
			var dateString = date.GetValue().ToString().Substring(0, 10).Replace(':', '/');
			DateTime.TryParse(dateString, out DateTime result);
			return result;
		}
		return null;
	}

	private void MoveFile(string from, string to)
	{
		string message = null;
		var fromForLogging = Path.GetFileName(from);
		var toForLogging = Path.GetDirectoryName(to).Split('\\').Last();
		if (!doIt)
		{
			message = $"    NOT Moving '{fromForLogging}' to '{toForLogging}'";
		}
		else
		{
			if (File.Exists(to))
			{
				if (overwrite)
				{
					message = $"    Moving '{fromForLogging}' to '{toForLogging}' (overwriting)";
					File.Move(from, to);

				} else
                {
					message = $"    File '{to}' exists, skipping.";
				}
			}
			else
			{
				message = $"    Moving '{fromForLogging}' to '{toForLogging}'";
				File.Move(from, to);
			}
		}
		if (verbosityLevel == VerbosityLevel.Verbose)
			Console.WriteLine(message);
	}

	private string MakeSureDirExists()
	{
		var year = shootingDate.Value.Year;
		var month = shootingDate.Value.Month;
		var day = shootingDate.Value.Day;
		var requestedActualDir = $"{year}.{month:D2}.{day:d2}";
		newDirectory = Path.Combine(directory, requestedActualDir);

		if (Directory.Exists(newDirectory)) return null;

		string actualDirname = null;
		var existingDirs = Directory.EnumerateDirectories(directory, requestedActualDir + "*");
		if (existingDirs.Any())
		{
			if (existingDirs.Count() == 1)
			{
				actualDirname = existingDirs.First();
			}
		}
		else
		{
			actualDirname = newDirectory;
		}
		if (actualDirname != null)
		{
			if (isPanorama)
			{
				newDirectory = Path.Combine(newDirectory, "pano");
			}

			if (verbosityLevel == VerbosityLevel.Verbose)
			{
				Console.WriteLine(doIt
					? $"  Creating folder '{newDirectory}'"
					: $"  NOT creating folder '{newDirectory}'");
			}
			if (doIt)
			{
				Directory.CreateDirectory(newDirectory);
			}
		}
		return actualDirname;
	}

	private bool IsPanorama(ExifProfile exifProfile)
	{
		var width = (Number)(exifProfile.Values.FirstOrDefault(e => e.Tag == ExifTag.PixelXDimension)?.GetValue());
		var height = (Number)(exifProfile.Values.FirstOrDefault(e => e.Tag == ExifTag.PixelYDimension)?.GetValue());
		var longer = (int)width;
		var shorter = (int)height;
		if (longer < shorter)
		{
			var temp = longer;
			longer = shorter;
			shorter = temp;
		}
		return longer / shorter > PanoramaRatio;
	}

	// I used it at some point in time
	public void CompareDirectories(string src, string dst)
	{
		var srcFiles = Directory.EnumerateFiles(src, "*.jp*g", SearchOption.AllDirectories);
		var dstFiles = new Dictionary<string, string>();
		foreach (var dstFile in Directory.EnumerateFiles(dst, "*.jp*g", SearchOption.AllDirectories))
		{
			dstFiles.Add(Path.GetFileNameWithoutExtension(dstFile), dstFile);
		}
		var found = new List<string>();
		var notFound = new List<string>();
		foreach (var srcFile in srcFiles)
		{
			var fileName = Path.GetFileNameWithoutExtension(srcFile);
			if (!dstFiles.ContainsKey(fileName))
			{
				notFound.Add(srcFile);
			}
			else
			{
				found.Add(srcFile);
			}
		}
		//found.Dump("Found");
		//notFound.Dump("NOT found");
	}
}
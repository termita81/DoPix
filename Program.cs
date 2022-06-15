void PrintHelp()
{
	var execName = AppDomain.CurrentDomain.FriendlyName;
	Console.WriteLine($@"
Run as:
	{execName} <directory> [-h|--help] [-vv|--verbose] [-k|--fake]
Arguments
	directory				required - which directory to process
							can use multiple directories

	--help					print this message and exit
	-h

	--fake					don't actually make any changes
	-k

	--overwrite				overwrite existing files
	-w

Sample runs
	{execName} .
	{execName} c:\photos\unsorted
	{execName} -h
	{execName} c:\photos\unsorted -vv --fake 
	{execName} . c:\photos\unsorted -vv --fake 
");
}

// --verbose           print more information
//	-vv

if (args.Length == 0)
{
	PrintHelp();
}
else
{
	var paths = new List<string>();
	var verbosityLevel = VerbosityLevel.None;
	var fakeIt = false;
	var overwrite = false;

	foreach (var arg in args)
	{
		switch (arg)
		{
			case "-vv":
			case "--verbose":
				verbosityLevel = VerbosityLevel.Verbose;
				break;
			case "-h":
			case "--help":
				PrintHelp();
				return;
			case "-k":
			case "--fake":
				fakeIt = true;
				break;
			case "-w":
			case "--overwrite":
				overwrite = true;
				break;
			default:
				paths.Add(Path.GetFullPath(arg));
				break;
		}
	}
	paths.ForEach(new Mover(!fakeIt, overwrite, verbosityLevel).SplitFilesToFoldersByDate);
}
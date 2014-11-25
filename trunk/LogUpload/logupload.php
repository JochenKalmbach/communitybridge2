<?php
/** Error reporting */
#error_reporting(E_ALL);
//date_default_timezone_set('Europe/Berlin');
# "constants"
$timestamp = date('YmdHis');
$smtp_class = "./libs/smtp";

$header = '
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="de" lang="de">
<head>
	<title>Community bridge 2</title>
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
</head>
<body style="overflow: auto";>
';

if (!isset($_POST['upload']))
{
	print $header;
	print "Only for internal use";
	/*print "
	<H2>Upload community bridge diagnose data</H2>
	<form name=myuploadform action=\"\" method=\"post\" enctype=\"multipart/form-data\">
	<table border=\"0\" cellpadding=\"5\" cellspacing=\"5\">
	<tr><td>&nbsp;</td></tr>
	<tr><td><b>File:</b> <input type=\"file\" name=\"NeueListe\" width=\"200\"></td></tr>
	<tr><td>&nbsp;</td></tr>
	<tr><td><button type=\"submit\" name=\"upload\" value=\"upload\">Upload</button></td></tr>
	<tr><td>&nbsp;</td></tr>
	</table>
	</form>\n";*/

print "</body></html>";
exit;
}

//print_r($_FILES);

if(isset($_FILES['NeueListe']) and ! $_FILES['NeueListe']['error'])
{
	//print_r($_FILES);
	//exit;
	;
}
else
{
	$error = $_FILES['NeueListe']['error'];
	if (isset($_POST['auto']))
	{
		print "Failed: upload failed; Error ".$error;
		exit;
	}
	print $header;
	print "<span class=\"red\">fail: upload failed; Error ".$error."</span><br />\n";
    print "</body></html>";
	exit;
}

$inputFileName = $_FILES['NeueListe']['name'];
$inputFilePath = $_FILES['NeueListe']['tmp_name'];
$inputFileSize = round($_FILES['NeueListe']['size']/1024,0);
$inputFileType = $_FILES['NeueListe']['type'];
if (!file_exists($inputFilePath))
	exit ("Failed: file $inputFilePath vanished!");
if ($inputFileSize > 8000)
	exit ("Failed: file too big!");
//if ($inputFileType != "text/plain")
if ($inputFileType != "application/x-zip-compressed")
	exit ("Failed: bad file type!");

# send the file
$filePart = array( "FileName" => $inputFilePath, "Name" => $inputFileName, "Content-Type" => "application/zip", "Disposition" => "attachment" );
require_once($smtp_class.'/email_message.php');
$new_message = new email_message_class;

$plainText = "Post data:\r\n";
$plainText .= print_r($_POST, true);


$new_message->SetEncodedEmailHeader('From', 'bridge@kalmbach.eu', 'Bridge 2 Logger', 'utf-8' );
$new_message->SetEncodedEmailHeader('To', 'bridge@kalmbach.eu', 'Bridge 2 Logger', 'utf-8' );

if (isset($_POST['userSendEmail']) && isset($_POST['userEmail']) && ($_POST['userSendEmail'] == 'True'))
{
  $new_message->SetEncodedEmailHeader('Cc', $_POST['userEmail'], $_POST['userEmail'], 'utf-8' );
}


$new_message->SetHeader('Subject', 'Log '.$timestamp, 'utf-8' );
$new_message->AddPlainTextPart($plainText, 'ascii' );
$new_message->AddFilePart($filePart);

$new_message->Send();
if ($new_message->error != "")
	$message_errors = $new_message->error;
else
{
	$message_errors = "ok, message sent to $EmailTo";
	if (isset($_POST['auto']))
	{
	  printf("OK");
	  exit;
    }
}
if (isset($_POST['auto']))
{
  printf($message_errors);
  exit;
}



print $header;
print "<h2>$inputFileName - $inputFileSize KB ($inputFileType)</h2>\n";
print "mailer: $message_errors<br />\n";
print "<br />\n";
print "</body></html>";

exit;
?>

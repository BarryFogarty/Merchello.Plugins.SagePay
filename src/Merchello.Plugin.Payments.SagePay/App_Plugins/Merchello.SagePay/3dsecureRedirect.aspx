<%@ Page Language="C#" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<script runat="server">
    public static string Base64Decode(string base64EncodedData)
    {
        byte[] base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }
</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body onload="document.form.submit();"> 
    <form id="form" name="form" action='<%=  Base64Decode(Request.QueryString["acsurl"]) %>' method="post" target="3dsecure">
    
        <input type="hidden" name="PaReq" value='<%= Base64Decode(Request.QueryString["PaReq"]) %>' />
        <input type="hidden" name="TermUrl" value='<%= Base64Decode(Request.QueryString["TermUrl"]) %>' />
        <input type="hidden" name="MD" value='<%= Base64Decode(Request.QueryString["MD"]) %>' />
        Please click the following button to submit your payment to 3dSecure
        <input type="submit" value="Submit Form" />
        </noscript>
        
    </form>
    
</body>
</html>

FOR /D /R . %%X IN (_ReSharper.*) DO RD /S /Q "%%X"
FOR /D /R . %%X IN (bin) DO RD /S /Q "%%X"
FOR /D /R . %%X IN (obj) DO RD /S /Q "%%X"
DEL /s *.DotSettings.*
DEL /s *.csproj.user
DEL /ah /s *.suo
namespace ChubDownloader.Models;

public class CharacterInfo
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Url { get; set; }
  public int ChatCount { get; set; }
  public string UserName { get; set; }
        
  public CharacterInfo(string id, string name, string url, int chatCount = 0, string userName = "")
  {
    Id = id;
    Name = name;
    Url = url;
    ChatCount = chatCount;
    UserName = userName;
  }
}
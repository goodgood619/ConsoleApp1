﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp1.Models;

namespace ConsoleApp1.Module
{
    public class MessengerServer : TcpServer
    {
        private const int serverport = 33212;
        private DBhelp dBhelp;
        private JsonHelp jsonHelp;
        public List<Clientdata> clientlist { get; set; }

        public MessengerServer()
        {
            //this.jsonHelp = new JsonHelp();
            this.dBhelp = new DBhelp();
            this.clientlist = new List<Clientdata>();
            //this.jsonHelp = new JsonHelp();
            Console.WriteLine($"Messenger Server Start : (Port: {serverport})");
        }
        public override List<SocketData> responseMessage(Socket socket, TCPmessage receivemessage)
        {
            List<SocketData> sendclient = new List<SocketData>();
            TCPmessage sendmessage = new TCPmessage();
            this.jsonHelp = new JsonHelp();
            switch (receivemessage.command)
            {
                case Command.login:
                    Dictionary<string, string> logininfo = jsonHelp.getlogininfo(receivemessage.message);
                    string id = logininfo[JsonName.ID];
                    string password = logininfo[JsonName.Password];

                    bool idcheck = dBhelp.IsexistID(id);
                    bool passcheck = dBhelp.IsExistPassword(password);
                    bool validlogin = dBhelp.validLogin(id, password);


                    if (!idcheck && !validlogin) sendmessage.check = 0;
                    if (!passcheck && !validlogin) sendmessage.check = 1;
                    if (validlogin)
                    {
                        bool duplicate = false;
                        foreach (Clientdata clientdata in clientlist) //중복로그인 방지
                        {
                            if (clientdata.id == id)
                            {
                                duplicate = true;
                                break;
                            }
                        }
                        if (duplicate) //중복로그인
                        {
                            sendmessage.check = 4;
                        }
                        else
                        {
                            clientlist.Add(new Clientdata(socket, id)); //서버에 login을 했으니 정보를 추가해줘야함
                            int usernumber = dBhelp.Getusernumber(id);
                            string nickname = dBhelp.Getnickname(usernumber);
                            sendmessage.Usernumber = usernumber;
                            sendmessage.message = jsonHelp.nickinfo(nickname);
                            sendmessage.check = 2;
                        }
                    }
                    if (!idcheck && !passcheck) sendmessage.check = 3;

                    sendmessage.command = Command.login;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Join:
                    Dictionary<string, string> joininfo1 = jsonHelp.getlogininfo(receivemessage.message);
                    Dictionary<string, string> joininfo2 = jsonHelp.getphonenick(receivemessage.message);
                    string joinid = joininfo1[JsonName.ID];
                    string joinpassword = joininfo1[JsonName.Password];
                    string joinnickname = joininfo2[JsonName.Nickname];
                    string joinphone = joininfo2[JsonName.Phone];
                    int joinusernumber = dBhelp.Getjoinusernumber();
                    if (!dBhelp.IsexistID(joinid))
                    {
                        dBhelp.join(joinid, joinpassword, joinnickname, joinphone, joinusernumber);
                        sendmessage.check = 1; //회원가입되었다는것을 의미
                    }
                    else sendmessage.check = 0;
                    sendmessage.command = Command.Join;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Idcheck:
                    Dictionary<string, string> idinfo = jsonHelp.getidinfo(receivemessage.message);
                    string checkid = idinfo[JsonName.ID];
                    if (!dBhelp.IsexistID(checkid))
                    {
                        sendmessage.check = 1;
                    }
                    else sendmessage.check = 0;
                    sendmessage.command = Command.Idcheck;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Nicknamecheck:
                    Dictionary<string, string> nickinfo = jsonHelp.getnickinfo(receivemessage.message);
                    string checknickname = nickinfo[JsonName.Nickname];
                    if (!dBhelp.Isexistnickname(checknickname))
                    {
                        sendmessage.check = 1;
                    }
                    else sendmessage.check = 0;
                    sendmessage.command = Command.Nicknamecheck;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.logout:
                    Dictionary<string, string> logoutinfo = jsonHelp.getnickinfo(receivemessage.message);
                    string logoutid = dBhelp.Getid(logoutinfo[JsonName.Nickname]);
                    Clientdata LogoutData = clientlist.Find(x => (x.id == logoutid));
                    clientlist.Remove(LogoutData);
                    sendmessage.command = Command.logout;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Findid:
                    Dictionary<string, string> findidinfo = jsonHelp.getidinfo(receivemessage.message);
                    string findid = findidinfo[JsonName.ID];
                    if (dBhelp.IsexistID(findid))
                    {
                        string findpassword = dBhelp.Findpass(findid);
                        sendmessage.message = jsonHelp.logininfo(findid, findpassword);
                        sendmessage.check = 1;
                    }
                    else sendmessage.check = 0;
                    sendmessage.command = Command.Findid;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Plusfriend:
                    Dictionary<string, string> plusidinfo1 = jsonHelp.getidinfo(receivemessage.message);
                    string userid = plusidinfo1[JsonName.ID];
                    Dictionary<string, string> plusidinfo2 = jsonHelp.getFnickinfo(receivemessage.message);
                    string plusid = plusidinfo2[JsonName.FID];
                    if (dBhelp.IsexistID(plusid)) // 아이디의 존재유무만 체크했지 아직 친구추가의 중복부분은 처리안함, 그 부분을 서버에서 가지고있어야함 친구목록을
                    {
                        if (plusid == userid) //추가하려는 아이디가 동일한경우
                        {
                            sendmessage.check = 2;
                        }
                        else if (!dBhelp.Plusid(plusid, userid)) //이미 친구를 추가한 아이디인 경우
                        {
                            sendmessage.check = 3;
                        }
                        else
                        {
                            string usernickname = dBhelp.Getnickname(userid);
                            string plusnickname = dBhelp.Getnickname(plusid);
                            dBhelp.plusfriend(userid, usernickname, plusid, plusnickname); //DB에 친구추가
                            sendmessage.message = jsonHelp.nickinfo(plusnickname);
                            sendmessage.check = 1;
                        }
                    }
                    else //아이디가 존재하지 않는 경우
                    {
                        sendmessage.check = 0;
                    }
                    sendmessage.command = Command.Plusfriend;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
                case Command.Refresh:
                    Dictionary<string, string> refreshinfo = jsonHelp.getnickinfo(receivemessage.message);
                    string refreshnickname = refreshinfo[JsonName.Nickname];
                    int refreshcnt = dBhelp.Refreshfriendcount(refreshnickname); //nickname의 친구명수
                    sendmessage.command = Command.Refresh;
                    sendmessage.Friendcount = refreshcnt;
                    sendclient.Add(new SocketData(socket, sendmessage));
                    break;
            }

            return sendclient;
        }
    }
}

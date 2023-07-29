#!/bin/bash

function print_help() {
    echo "用法：sh fastapi.sh [命令] [端口]";
    echo "命令："
    echo "      install 安装服务";
    echo "      start   启动服务";
    echo "      stop    停止服务";
    echo "      list    查看服务";
    echo "端口："
    echo "      start、stop命令可用，任意数字，默认8080"
    echo
}

if [[ $# == 0 ]] || [[ $# > 2 ]] || ( [[ $# -gt 1 ]] && [[ "$1" == "install" || "$1" == "list" ]] ); then
    print_help
    exit
fi

cmd=$1
port=$2

if [ $cmd == "install" ]; then
    sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    sudo yum install -y dotnet-sdk-3.1  --nogpgcheck
elif [ $cmd == "start" ]; then
    if [ "$port" == "" ]; then
        port=8080
    fi

    PID=`ps -ef | grep "dotnet fastapi.dll env=Production port=$port$" | grep -v grep | awk '{print $2}'`
    if [ $PID ]; then
        echo "服务已在$port端口运行，请不要重复启动。"
    else
        nohup dotnet fastapi.dll env=Production port=$port >nohup.log 2>&1 </dev/null &
    fi
elif [ $cmd == "stop" ]; then
    if [ "$port" == "" ]; then
        PID=`ps -ef | grep fastapi.dll | grep -v grep | awk '{print $2}'`
        for item in $PID
        do
            kill -s 9 $item
        done
    else
        PID=`ps -ef | grep "dotnet fastapi.dll env=Production port=$port$" | grep -v grep | awk '{print $2}'`
        for item in $PID
        do
            kill -s 9 $item
        done
    fi
elif [ $cmd == "list" ]; then
    ps -aux | grep fastapi.dll | grep -v grep
fi

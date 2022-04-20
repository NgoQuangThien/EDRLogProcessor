# EndpointLogProcessor

##	Cài đặt dịch vụ
```
	1.	Copy thư mục EDRProcessor vào ổ C:
	2.	Mở cmd bằng quyền Administrator
	3.	Di chuyển cmd tới folder "C:\Windows\Microsoft.NET\Framework64\v4.xxx\" v4.xxx là folder .NET cao nhất
	4.	Thực thi lệnh: InstallUtil.exe "C:\EDRProcessor\bin\Debug\EDRProcessor.exe"
```

##	Bắt đầu dịch vụ
```
	Khởi động dịch vụ thông qua "Services" của Windows với tên dịch vụ là "EDRProcessor"
```

##	EventLog và ServiceLog
```
	Trong "C:\EDRProcessor\" có chứa 2 thư mục EventLog và ServiceLog
	1.	EventLog: là thư mục lưu trữ các sự kiện của Endpoint đã được phân tích cú pháp
	2.	ServiceLog: là thư mục lưu trữ nhật ký hoạt động của dịch vụ
```

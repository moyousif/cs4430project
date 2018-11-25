create database swlicensing;

use swlicensing;

create table users (
 userid varchar(10) primary key,
 pwd varchar(10) not null,
 fname varchar(20) not null,
 lname varchar(20) not null,
 login_time datetime
);

create table roles (
 userid varchar(10) primary key,
 role varchar(10) not null
);

create table department (
 name varchar(20) primary key,
 cost_center int unsigned unique,
 budget float,
 members varchar(10)
);

create table software (
 sw_name varchar(20) primary key,
 fullpath varchar(120) not null
);

create table license (
 sw_name varchar(20) not null,
 cost_center int unsigned not null,
 available int unsigned not null,
 cost_per_user float not null
);

create table sw_usage (
 sw_name varchar(20) not null,
 userid varchar(10) not null,
 start_time datetime not null,
 end_time datetime,
 primary key(sw_name,userid,start_time)
);

create table request (
 userid varchar(10) not null,
 sw_name varchar(20) not null,
 request_type enum('add','remove'),
 primary key(sw_name,userid)
);

delimiter //
create trigger usage_cleanup after update on users 
for each row
begin
    update sw_usage
    set end_time = new.login_time
    where userid = new.userid;
end;
//
delimiter ;
